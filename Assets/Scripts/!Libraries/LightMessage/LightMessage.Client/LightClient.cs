using LightMessage.Common.Connection;
using LightMessage.Common.ProtocolMessages;
using LightMessage.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightMessage.Client
{
    public class LightClient
    {
        class ConnectionCallbacks : IConnectionCallbacks
        {
            LightClient Owner;

            public ConnectionCallbacks(LightClient Owner)
            {
                this.Owner = Owner;
            }

            public void OnDisconnect()
            {
                if (Owner.SessionId != null) // Otherwise, we're just disconnecting ourselves
                {
                    if (Owner.Logger.IsWarning()) Owner.Logger.Warn("Connection failed, attempting to reconnect");
                    Owner.Reconnect();
                }
            }

            public void OnNewInvocation(InvocationMessageBase Message)
            {
                if (Owner.Logger.IsVerbose()) Owner.Logger.Verbose($"Invocation received from server: {Message}");
                Owner.InternalOnInvocation(Message);
            }

            public void TerminateSession()
            {
                if (Owner.Logger.IsWarning()) Owner.Logger.Warn("Session was terminated by the persistent connection module");
                Owner.DisconnectInternal(false);
            }
        }


        public bool IsConnected
        {
            get
            {
                return Connection != null && Connection.IsConnected();
            }
        }

        public bool IsConnectedOrReconnecting { get; private set; } = false;

        public Guid? SessionId { get; private set; } = null;

        public delegate void ClientOnAnyInvocationDelegate(InvocationMessageBase Message);
        public event ClientOnAnyInvocationDelegate OnAnyInvocation;

        public delegate void OnSessionTerminatedDelegate(bool WasCleanShutdown);
        public event OnSessionTerminatedDelegate OnSessionTerminated;

        TcpClient TcpClient;
        IPEndPoint ServerEndPoint;
        ReliableConnection Connection;

        DateTime NextKeepAliveTime;
        TimeSpan KeepAliveTime;
        CancellationTokenSource KeepAliveCancellationTokenSource;

        bool bConnecting = false;

        internal Logger Logger { get; private set; }


        public LightClient(ILogProvider LogProvider = null)
        {
            Logger = new Logger(LogProvider ?? new NullLogProvider());
        }

        protected virtual void InternalOnInvocation(InvocationMessageBase Message)
        {
            OnAnyInvocation?.Invoke(Message);
        }

        public async Task Connect(IPEndPoint IPEndPoint, CancellationToken CancellationToken, AuthRequestMessage AuthMessage)
        {
            Logger.Info($"Starting client for server address {IPEndPoint}");

            if (SessionId != null || Connection != null || bConnecting)
            {
                Logger.Error("Client already connecting or previously connected");
                throw new InvalidOperationException("Already connected or connecting, can't connect again");
            }

            try
            {
                bConnecting = true;

                TcpClient = new TcpClient();
                ServerEndPoint = IPEndPoint;

                if (Logger.IsVerbose()) Logger.Verbose($"TCP connecting to server");
                await TcpClient.ConnectAsync(IPEndPoint.Address, IPEndPoint.Port);

                if (Logger.IsVerbose()) Logger.Verbose("Sending authentication message");
                if (Logger.IsVerbose()) Logger.Verbose($"Authentication message contents: {AuthMessage}");
                await AuthMessage.Send(TcpClient);

                MessageBase Message;
                var CTS = new CancellationTokenSource(ClientConfig.AuthResponseTimeoutMilliseconds);
                try
                {
                    if (Logger.IsVerbose()) Logger.Verbose("Waiting for authentication response");
                    Message = await MessageBase.ReadFrom(TcpClient.GetStream(), Config.MessageMaxSize, Config.MessageMaxLengthSectionBytes, CTS.Token);
                    if (Logger.IsVerbose()) Logger.Verbose($"Authentication response received: {Message}");
                }
                catch (Exception Ex)
                {
                    if (Logger.IsError()) Logger.Error($"Failed to receive authentication response due to exception: {Ex}");
                    TcpClient.Dispose();
                    throw new TimeoutException();
                }

                switch (Message.GetMessageType())
                {
                    case MessageType.AuthResponse:
                        break;

                    case MessageType.AuthFailure:
                        if (Logger.IsError()) Logger.Error("Server refused to authenticate with provided credentials");
                        throw new AuthenticationFailedException();

                    default:
                        var ErrorDesc = $"The server responded with a message of type {Message.GetMessageType()} while we were expecting an authentication response.";
                        if (Logger.IsError()) Logger.Error(ErrorDesc);
                        throw new InvalidOperationException(ErrorDesc);
                }

                var AuthResponseMessage = Message as AuthResponseMessage;
                if (Logger.IsVerbose()) Logger.Verbose($"Received authentication response: {AuthResponseMessage}");
                KeepAliveTime = AuthResponseMessage.KeepAliveTime;
                SessionId = AuthResponseMessage.SessionId;
                Connection = new ReliableConnection(new ConnectionCallbacks(this), AuthResponseMessage.RequestAckTimeout, AuthResponseMessage.ReconnectTimeout, AuthResponseMessage.KeepAliveTimeout, Logger, "Client connection");
                Connection.OnConnect(TcpClient);

                if (Logger.IsVerbose()) Logger.Verbose($"Sending ready message");
                await new ReadyMessage().Send(TcpClient);
                if (Logger.IsVerbose()) Logger.Verbose($"Ready message sent");

                SendKeepAlive();

                IsConnectedOrReconnecting = true;
                if (Logger.IsVerbose()) Logger.Verbose($"Sending ready message");

                Logger.Info($"Connected to server with session ID {SessionId}");
            }
            finally
            {
                bConnecting = false;
            }
        }

        internal async void Reconnect()
        {
            Logger.Info("Starting reconnection sequence");

            if (!SessionId.HasValue || Connection == null)
            {
                Logger.Error("Cannot reconnect without a valid Session ID or Connection object");
                throw new InvalidOperationException("Cannot reconnect without a valid Session ID or Connection object");
            }

            if (bConnecting)
            {
                Logger.Info("Reconnection already in progress, won't attempt again");
                return;
            }

            try
            {
                bConnecting = true;
                for (int Retries = 0; Retries < ClientConfig.ReconnectMaxRetries; ++Retries)
                {
                    if (Logger.IsVerbose()) Logger.Verbose($"Reconnect attemp #{Retries + 1}");
                    try
                    {
                        if (TcpClient != null)
                            TcpClient.Dispose();

                        TcpClient = new TcpClient();

                        if (Logger.IsVerbose()) Logger.Verbose("TCP connecting to server");
                        await TcpClient.ConnectAsync(ServerEndPoint.Address, ServerEndPoint.Port);

                        if (Logger.IsVerbose()) Logger.Verbose($"Sending rejoin message with session ID {SessionId}");
                        await new AuthRejoinMessage(SessionId.Value).Send(TcpClient);

                        if (Logger.IsVerbose()) Logger.Verbose("Waiting for rejoin response");
                        var CTS = new CancellationTokenSource(ClientConfig.AuthResponseTimeoutMilliseconds);
                        MessageBase Message;
                        try
                        {
                            Message = await MessageBase.ReadFrom(TcpClient.GetStream(), Config.MessageMaxSize, Config.MessageMaxLengthSectionBytes, CTS.Token);
                        }
                        catch
                        {
                            TcpClient.Dispose();
                            throw;
                        }

                        if (Logger.IsVerbose()) Logger.Verbose($"Received rejoin response from server {Message}");

                        switch (Message.GetMessageType())
                        {
                            case MessageType.AuthResponse:
                                SessionId = (Message as AuthResponseMessage).SessionId;
                                break;

                            case MessageType.AuthFailure:
                                DisconnectInternal(false);
                                return;

                            default:
                                throw new InvalidOperationException($"The server responded with a message of type {Message.GetMessageType()} while we were expecting an authentication response.");
                        }

                        if (Logger.IsVerbose()) Logger.Verbose("Sending ready message");
                        await new ReadyMessage().Send(TcpClient);

                        await Connection.OnReconnect(TcpClient);

                        if (Logger.IsVerbose()) Logger.Verbose("Reconnection complete");
                        return;
                    }
                    catch (Exception Ex)
                    {
                        Logger.Error($"Failed to reconnect to server due to exception: {Ex}");
                    }
                }

                Logger.Error($"Failed to reconnect after {ClientConfig.ReconnectMaxRetries} attempts, terminating session");
                DisconnectInternal(false);
            }
            finally
            {
                bConnecting = false;
            }
        }

        async void SendKeepAlive()
        {
            if (Logger.IsInfo()) Logger.Info("Starting keep-alive task");

            try
            {
                KeepAliveCancellationTokenSource = new CancellationTokenSource();
                NextKeepAliveTime = DateTime.Now;

                while (true)
                {
                    NextKeepAliveTime = NextKeepAliveTime.Add(KeepAliveTime);

                    var WaitTime = NextKeepAliveTime - DateTime.Now;
                    if (WaitTime > TimeSpan.Zero)
                        await Task.Delay(WaitTime, KeepAliveCancellationTokenSource.Token);

                    if (Logger.IsVerbose()) Logger.Verbose("Sending keep-alive");

                    if (Connection != null)
                        Connection.CheckAndSendKeepAlive();
                }
            }
            catch { }
        }

        public void DelayKeepAlive(TimeSpan Delay)
        {
            Connection?.DelayKeepAlive(Delay);
        }

        public Task<ulong> SendInvocation(InvocationMessageBase Message, CancellationToken CancellationToken)
        {
            if (Logger.IsVerbose()) Logger.Verbose($"Sending invocation to server: {Message}");
            return Connection.SendInvocation(Message, CancellationToken);
        }

        protected virtual void DisconnectInternal(bool Clean)
        {
            if (Logger.IsInfo()) Logger.Info("Starting disconnection sequence");

            IsConnectedOrReconnecting = false;

            if (Logger.IsVerbose()) Logger.Verbose("Cancelling keep-alive task");
            KeepAliveCancellationTokenSource?.Cancel();

            if (Logger.IsVerbose()) Logger.Verbose("Disconnecting client");
            if (Connection != null)
                Connection.CleanDisconnect(TimeSpan.FromMilliseconds(ClientConfig.DisconnectTimeoutMilliseconds)).DontCare();

            Connection = null;
            SessionId = null;
            ServerEndPoint = null;

            if (OnSessionTerminated != null)
            {
                if (Logger.IsVerbose()) Logger.Verbose("Raising session termination event");
                OnSessionTerminated.Invoke(Clean);
            }
            else
                if (Logger.IsVerbose()) Logger.Verbose("No listener subscribed to session termination event");

            if (Logger.IsVerbose()) Logger.Verbose("Disconnected");
        }

        public void Disconnect()
        {
            DisconnectInternal(true);
        }
    }
}
