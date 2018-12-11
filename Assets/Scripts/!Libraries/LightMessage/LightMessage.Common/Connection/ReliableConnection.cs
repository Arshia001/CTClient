using LightMessage.Common.ProtocolMessages;
using LightMessage.Common.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightMessage.Common.Connection
{
    // Lesson from SignalR source: Do not store messages with connections.
    // Currently we probably won't have (too m)any broadcast messages, but 
    // if we do, we should store the messages in some middle layer, and 
    // have the connections subscribe to those.
    // We should probably create a more intelligent message store than the
    // current dictionary scheme if we need to expand this code later on.
    public class ReliableConnection
    {
        public DateTime? ReconnectDeadline { get; private set; }

        public bool IsStopped { get; set; } = false;


        IConnectionCallbacks Callbacks;

        TcpClient TcpClient;
        SemaphoreSlim SendLock;

        ulong NextID = 1;
        long LastReceivedID = 0; // If messages arrive out of order, we will simply terminate the connection, since it's required by the protocol for them to arrive in order
        SpinLock LastReceivedIDUpdateLock = new SpinLock(false);
        DateTime NextKeepAliveDeadline;

        ConcurrentDictionary<ulong, TaskCompletionSource<object>> PendingMessages = new ConcurrentDictionary<ulong, TaskCompletionSource<object>>();
        ConcurrentDictionary<ulong, InvocationMessageBase> CachedInvocations = new ConcurrentDictionary<ulong, InvocationMessageBase>();

        TimeSpan RequestACKTimeout;
        TimeSpan ReconnectTimeout;
        TimeSpan KeepAliveTimeout;

        Logger Logger;
        string ConnectionNameForLog;


        public ReliableConnection(IConnectionCallbacks Callbacks, TimeSpan RequestACKTimeout, TimeSpan ReconnectTimeout, TimeSpan KeepAliveTimeout, Logger Logger, string ConnectionNameForLog)
        {
            this.Callbacks = Callbacks;

            this.RequestACKTimeout = RequestACKTimeout;
            this.ReconnectTimeout = ReconnectTimeout;
            this.KeepAliveTimeout = KeepAliveTimeout;

            this.Logger = Logger;
            this.ConnectionNameForLog = ConnectionNameForLog;
        }

        public void OnConnect(TcpClient TcpClient)
        {
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Connected");

            this.TcpClient = TcpClient;
            SendLock = new SemaphoreSlim(1);
            ListenForClientMessages();
            OnKeepAlive();
        }

        public async Task OnReconnect(TcpClient TcpClient)
        {
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} TCP reconnected");

            ReconnectDeadline = null;

            if (this.TcpClient != null)
                this.TcpClient.Dispose();
            this.TcpClient = TcpClient;

            // Should start listening before we send, since we need to receive both the remote's cached messages and the ACKs to our own
            ListenForClientMessages();

            OnKeepAlive();

            try
            {
                await SendLock.WaitAsync();
                if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Sending previously cached invocations");
                foreach (var KV in CachedInvocations.ToArray())
                {
                    var InvocationID = KV.Key;
                    var Invocation = KV.Value;

                    if (CanWriteToNetwork())
                    {
                        TaskCompletionSource<object> TCS;
                        // There should already be a TCS for any cached send which we (and the caller) will wait on.
                        if (!PendingMessages.TryGetValue(InvocationID, out TCS))
                        {
                            if (Logger.IsError()) Logger.Error($"{ConnectionNameForLog} Internal Error - No pending TCS found for invocation {Invocation} with ID {InvocationID}");
                            InvocationMessageBase unused;
                            CachedInvocations.TryRemove(InvocationID, out unused);
                            continue;
                        }

                        if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Sending cached invocation {Invocation} with ID {InvocationID}");
                        // If transmit fails, we will get an exception, which we don't catch on purpose
                        await InternalSendInvocation(Invocation, TCS, CancellationToken.None, false);

                        if (TCS.Task.Status == TaskStatus.RanToCompletion)
                        {
                            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Sent invocation with ID {InvocationID}");
                            InvocationMessageBase unused;
                            CachedInvocations.TryRemove(InvocationID, out unused);
                        }
                        else
                        {
                            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Failed to send cached invocation with ID {InvocationID}");
                            throw new Exception("Failed to send cached invocation");
                        }
                    }
                }
            }
            catch
            {
                ReconnectDeadline = DateTime.Now.Add(ReconnectTimeout);
                throw;
            }
            finally
            {
                SendLock.Release();
            }

            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Reconnection complete");
        }

        // Called externally to completely stop the connection
        public Task CleanDisconnect(TimeSpan Timeout)
        {
            if (CanWriteToNetwork())
            {
                if (Logger.IsInfo()) Logger.Info($"{ConnectionNameForLog} Stopping connection cleanly");
                var CTS = new CancellationTokenSource(Timeout);
                return new CleanDisconnectMessage().Send(TcpClient, CTS.Token).ContinueWith(t => Stop());
            }

            Stop();
            return Task.CompletedTask;
        }

        void Disconnect()
        {
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Attempting to disconnect");

            // The remote won't send ACKs after a reconnect, so we shouldn't wait for any
            if (TcpClient != null)
                TcpClient.Dispose();
            TcpClient = null;

            if (ReconnectDeadline == null)
            {
                ReconnectDeadline = DateTime.Now.Add(ReconnectTimeout);
                Callbacks.OnDisconnect();
            }
        }

        // Called externally to completely stop the connection
        public void Stop()
        {
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Stopping");

            if (IsStopped)
                return;

            IsStopped = true;

            Disconnect();

            if (SendLock != null)
                SendLock.Dispose();
            SendLock = null;

            ReconnectDeadline = null;

            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Clearing and cancelling pending messages");
            foreach (var TCS in PendingMessages.Values)
                TCS.TrySetCanceled();
            PendingMessages.Clear();

            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Clearing cached invocations");
            CachedInvocations.Clear();
        }

        // Called internally when reconnection is impossible, will also raise an event
        void Terminate()
        {
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Terminating");

            Stop();

            Callbacks.TerminateSession();
        }


        async void ListenForClientMessages()
        {
            var Stream = TcpClient.GetStream();

            try
            {
                if (Logger.IsInfo()) Logger.Info($"{ConnectionNameForLog} Listening for messages");
                while (CanReadFromNetwork())
                {
                    var Message = await MessageBase.ReadFrom(Stream, Config.MessageMaxSize, Config.MessageMaxLengthSectionBytes, CancellationToken.None);
                    if (Message == null) // Something went wrong and we received a bad message
                    {
                        if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Failed to receive message from network, will disconnect");
                        // forcefully disconnect the remote, as the state of the packets will be unknown to us
                        Disconnect();
                        return;
                    }

                    if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Received message {Message}");

                    if (Message is InvocationMessageBase)
                    {
                        var invMessage = (InvocationMessageBase)Message;
                        if (ProcessInvocationAndCheckShouldForwardToOuter(invMessage))
                            try
                            {
                                Callbacks.OnNewInvocation(invMessage);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Exception in external invocation handler: " + ex.ToString());
                            }
                    }
                    else
                    {
                        switch (Message.GetMessageType())
                        {
                            case MessageType.Ack:
                                OnAck(Message as AckMessage);
                                break;

                            case MessageType.KeepAlive:
                                OnKeepAlive((Message as KeepAliveMessage).NumSecondsUntilNextKeepAlive);
                                break;

                            case MessageType.CleanDisconnect:
                                if (Logger.IsInfo()) Logger.Info($"{ConnectionNameForLog} Remote has requested termination of the connection");
                                Terminate();
                                break;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                if (Logger.IsError()) Logger.Error($"{ConnectionNameForLog} Failed to listen due to exception {Ex}");
                Disconnect();
            }
        }

        bool CanReadFromNetwork() => TcpClient != null && TcpClient.Connected && TcpClient.GetStream().CanRead;

        bool ProcessInvocationAndCheckShouldForwardToOuter(InvocationMessageBase Message)
        {
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Sending Ack for message ID {Message.ID}");
            InternalSendMessage(new AckMessage(Message.ID), CancellationToken.None, false).DontCare();

            bool LockTaken = false;
            LastReceivedIDUpdateLock.Enter(ref LockTaken);

            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Checking invocation {Message}");
            try
            {
                var LastID = LastReceivedID;
                LastReceivedID = Math.Max(LastReceivedID, (long)Message.ID);

                // Messages always arrive in ascending order. So unless there was a disconnect and the remote
                // is sending cached messages (in which case they'll have to start from some number and do it
                // sequentially anyway), the IDs should always be sequential.
                if (LastReceivedID > 0 /* not the first message after a disconnect */ && LastReceivedID < (long)Message.ID - 1)
                {
                    if (Logger.IsWarning()) Logger.Warn($"{ConnectionNameForLog} Invocation arrived out of order, was expecting ID {LastReceivedID + 1} at most");

                    Callbacks.TerminateSession();
                    return false;
                }

                var Result = LastID < (long)Message.ID;
                if (Logger.IsVerbose()) Logger.Verbose(Result ? $"{ConnectionNameForLog} Invocation {Message} will be processed" : $"{ConnectionNameForLog} Invocation {Message} is a duplicate");
                return Result;
            }
            finally
            {
                LastReceivedIDUpdateLock.Exit();
            }
        }

        void OnAck(AckMessage Message)
        {
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Received Ack for {Message.InvocationId}");
            TaskCompletionSource<object> TCS;
            if (PendingMessages.TryRemove(Message.InvocationId, out TCS))
                TCS.SetResult(null);
            else if (Logger.IsWarning()) Logger.Warn($"{ConnectionNameForLog} Received Ack for unknown message with ID {Message.InvocationId}");
        }

        void OnKeepAlive(ulong? NumSecondsUntilNextKeepAlive = null)
        {
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Received keep-alive");
            if (NumSecondsUntilNextKeepAlive.HasValue)
            {
                if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Delaying keep-alive for {NumSecondsUntilNextKeepAlive} seconds on remote's request");
                SetKeepAliveDeadline(DateTime.Now + TimeSpan.FromSeconds(NumSecondsUntilNextKeepAlive.Value));
            }
            else
                SetKeepAliveDeadline(DateTime.Now + KeepAliveTimeout);
        }

        void SetKeepAliveDeadline(DateTime Deadline)
        {
            if (NextKeepAliveDeadline < Deadline)
                NextKeepAliveDeadline = Deadline;
        }

        // Must be called externally every keepalive cycle
        // This is an optimization allowing us to have only one task
        // for keepalives in the entire server
        public void CheckAndSendKeepAlive()
        {
            if (ReconnectDeadline != null)
            {
                if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Waiting for reconnect, won't check keep-alive");
                return;
            }

            // First, we check if we have received keep-alive in a timely manner
            if (DateTime.Now > NextKeepAliveDeadline)
            {
                if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Keep-alive not received in timely manner, will reconnect");
                Disconnect();
            }

            // Next, we send it ourselves
            if (CanWriteToNetwork())
            {
                if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Sending keep-alive");
                new KeepAliveMessage().Send(TcpClient).DontCare();
            }
        }

        public void DelayKeepAlive(TimeSpan Delay)
        {
            SetKeepAliveDeadline(DateTime.Now.Add(Delay));
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Delaying keep-alive for {Delay}");
            new KeepAliveMessage((ulong)Delay.TotalSeconds).Send(TcpClient).DontCare();
        }


        public async Task<ulong> SendInvocation(InvocationMessageBase Message, CancellationToken CancellationToken)
        {
            if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Sending invocation {Message}");

            if (PendingMessages.Count >= Config.RequestMaxConcurrentActive)
            {
                if (Logger.IsWarning()) Logger.Warn($"{ConnectionNameForLog} Too many concurrent invocations, will terminate");
                Terminate();
                throw new InvalidOperationException("Too many concurrent invocations");
            }

            var TCS = new TaskCompletionSource<object>();

            var messageID = await InternalSendInvocation(Message, TCS, CancellationToken, true);
            await TCS.Task;
            return messageID;

            //return InternalSendInvocation()
            //    .ContinueWith(t =>
            //    {
            //        if (t.Exception != null)
            //            throw t.Exception;
            //        return TCS.Task;
            //    }).Unwrap()
            //    .ContinueWith(t => t.Result);
        }

        async Task<ulong> InternalSendInvocation(InvocationMessageBase Message, TaskCompletionSource<object> TCS, CancellationToken CancellationToken, bool UseLockAndCache)
        {
            bool Success = false;

            try
            {
                if (UseLockAndCache)
                    await SendLock.WaitAsync(CancellationToken);

                if (Message.ID == 0)
                    Message.ID = NextID++;

                PendingMessages.TryAdd(Message.ID, TCS); // This will fail when the method is called during reconnect, which is normal

                if (CanWriteToNetwork())
                {
                    var NetworkStream = TcpClient.GetStream();

                    if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Sending invocation {Message} with newly allocated ID {Message.ID}");

                    var MessageData = Message.Serialize();

                    await NetworkStream.WriteAsync(MessageData.Array, MessageData.Offset, MessageData.Count);

                    if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Invocation with ID {Message.ID} was written to the network");

                    Success = true;
                }
            }
            catch (Exception Ex)
            {
                if (Logger.IsWarning()) Logger.Warn($"{ConnectionNameForLog} Failed to write invocation with ID {Message.ID} to the network due to exception: {Ex}");
                if (!UseLockAndCache)
                    throw;
            }
            finally
            {
                if (UseLockAndCache)
                    SendLock.Release();
            }

            // If message was written to network successfully, wait until ACK is recieved
            if (Success)
            {
                if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Waiting for ACK of {Message.ID}");

                await Task.WhenAny(TCS.Task, Task.Delay(RequestACKTimeout));

                if (TCS.Task.Status != TaskStatus.RanToCompletion)
                {
                    if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} ACK of {Message.ID} failed");
                    Success = false;
                }
            }

            if (!Success && UseLockAndCache)
            {
                // Failed to send message, disconnect now (outer layer is responsible for reconnecting) and cache the message
                if (CachedInvocations.Count < Config.RequestMaxCacheEntries)
                {
                    if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Caching invocation {Message}");
                    CachedInvocations.TryAdd(Message.ID, Message);
                    Disconnect();
                }
                else
                {
                    if (Logger.IsWarning()) Logger.Warn($"{ConnectionNameForLog} Too many cache entries, will terminate");
                    Terminate();
                    throw new InvalidOperationException("Too many cache entries");
                }
            }

            return Message.ID;
        }

        async Task InternalSendMessage(MessageBase Message, CancellationToken CancellationToken, bool UseLock)
        {
            var MessageData = Message.Serialize();

            try
            {
                if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Writing {Message} to network");

                var NetworkStream = TcpClient.GetStream();

                if (UseLock)
                    await SendLock.WaitAsync(CancellationToken);

                await NetworkStream.WriteAsync(MessageData.Array, MessageData.Offset, MessageData.Count);

                if (Logger.IsVerbose()) Logger.Verbose($"{ConnectionNameForLog} Message {Message} written to network");
            }
            catch (Exception Ex)
            {
                if (Logger.IsWarning()) Logger.Warn($"{ConnectionNameForLog} Failed to write message to network due to exception: {Ex}");
                throw;
            }
            finally
            {
                if (UseLock)
                    SendLock.Release();
            }
        }

        bool CanWriteToNetwork() => TcpClient != null && TcpClient.Connected && TcpClient.GetStream().CanWrite;

        public bool IsConnected() => TcpClient != null && TcpClient.Connected;
    }
}
