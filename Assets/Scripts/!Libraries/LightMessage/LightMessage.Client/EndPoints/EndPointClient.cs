using LightMessage.Common.Connection;
using LightMessage.Common.Messages;
using LightMessage.Common.ProtocolMessages;
using LightMessage.Common.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightMessage.Client.EndPoints
{
    public class EndPointClient : LightClient
    {
        SpinLock ReplyLock = new SpinLock();
        Dictionary<ulong, TaskCompletionSource<InvocationSuccessReplyMessage>> PendingReplies = new Dictionary<ulong, TaskCompletionSource<InvocationSuccessReplyMessage>>();
        Dictionary<ulong, InvocationReplyMessage> OrphanReplies = new Dictionary<ulong, InvocationReplyMessage>();
        ConcurrentDictionary<string, EndPointProxy> LocalEndPoints = new ConcurrentDictionary<string, EndPointProxy>();


        public EndPointClient(ILogProvider LogProvider = null) : base(LogProvider) { }

        public EndPointProxy CreateProxy(string EndPointName)
        {
            return LocalEndPoints.GetOrAdd(EndPointName, s => new EndPointProxy(this, EndPointName, Logger));
        }

        internal Task Register(EndPointProxy EndPoint)
        {
            return SendInvocationForReply(new InvocationMessage("$reg", "register", Param.Array(Param.String(EndPoint.EndPointName))), CancellationToken.None);
        }

        internal Task Unregister(EndPointProxy EndPoint)
        {
            EndPointProxy unused;
            LocalEndPoints.TryRemove(EndPoint.EndPointName, out unused);
            return base.SendInvocation(new InvocationMessage("$reg", "unregister", Param.Array(Param.String(EndPoint.EndPointName))), CancellationToken.None);
        }

        public async Task Connect(IPEndPoint IPEndPoint, CancellationToken CancellationToken, AuthRequestMessage AuthMessage, bool RegisterEndPointProxies)
        {
            await Connect(IPEndPoint, CancellationToken, AuthMessage);

            if (RegisterEndPointProxies)
                try
                {
                    await SendInvocationForReply(new InvocationMessage("$reg", "register", Param.Array(
                        LocalEndPoints.Values.Select(ep => Param.String(ep.EndPointName))
                        )), CancellationToken.None);
                }
                catch
                {
                    DisconnectInternal(false);
                    throw;
                }
        }

        internal Task<InvocationSuccessReplyMessage> SendInvocationForReply(InvocationMessage Message, CancellationToken CancellationToken)
        {
            return base.SendInvocation(Message, CancellationToken).ContinueWith(t =>
            {
                var ID = t.Result;
                bool lockTaken = false;
                ReplyLock.Enter(ref lockTaken);
                try
                {
                    if (OrphanReplies.ContainsKey(ID))
                    {
                        if (Logger.IsVerbose()) Logger.Verbose($"Already have reply for invocation with ID {ID}, will use that");
                        var result = OrphanReplies[ID];
                        OrphanReplies.Remove(ID);

                        switch (result.GetMessageType())
                        {
                            case MessageType.Invocation_SuccessReply:
                                return Task.FromResult(result as InvocationSuccessReplyMessage);

                            case MessageType.Invocation_FailureReply:
                                throw new InvocationFailureException(result as InvocationFailureReplyMessage);

                            default:
                                throw new InvalidOperationException($"Unknown reply message type {Message.GetMessageType()}");
                        }
                    }

                    if (Logger.IsVerbose()) Logger.Verbose($"Will wait for reply for invocation with ID {ID}");
                    var TCS = new TaskCompletionSource<InvocationSuccessReplyMessage>();
                    PendingReplies.Add(ID, TCS);
                    return TCS.Task;
                }
                finally
                {
                    ReplyLock.Exit();
                }
            }).Unwrap();
        }

        internal Task SendInvocation(InvocationMessage Message, CancellationToken CancellationToken)
        {
            return base.SendInvocation(Message, CancellationToken);
        }

        protected override void InternalOnInvocation(InvocationMessageBase Message)
        {
            if (Message is InvocationReplyMessage)
            {
                var InvocationID = (Message as InvocationReplyMessage).InvocationMessageID;
                bool lockTaken = false;
                TaskCompletionSource<InvocationSuccessReplyMessage> TCS = null;
                ReplyLock.Enter(ref lockTaken);
                try
                {
                    if (PendingReplies.ContainsKey(InvocationID))
                    {
                        if (Logger.IsVerbose()) Logger.Verbose($"Received reply with request ID {InvocationID} but a matching request was not found; will cache");

                        TCS = PendingReplies[InvocationID];
                        PendingReplies.Remove(InvocationID);
                    }
                    else
                    {
                        if (Logger.IsVerbose()) Logger.Verbose($"Received reply with request ID {InvocationID} but a matching request was not found; will cache");

                        OrphanReplies[InvocationID] = Message as InvocationReplyMessage;
                    }
                }
                finally
                {
                    ReplyLock.Exit();
                }

                if (TCS != null)
                    switch (Message.GetMessageType())
                    {
                        case MessageType.Invocation_SuccessReply:
                            TCS.SetResult(Message as InvocationSuccessReplyMessage);
                            return;

                        case MessageType.Invocation_FailureReply:
                            TCS.SetException(new InvocationFailureException(Message as InvocationFailureReplyMessage));
                            return;

                        default:
                            TCS.SetException(new InvalidOperationException($"Unknown reply message type {Message.GetMessageType()}"));
                            return;
                    }

            }
            else if (Message is InvocationMessage)
            {
                var InvMessage = Message as InvocationMessage;

                EndPointProxy EndPointProxy;
                if (LocalEndPoints.TryGetValue(InvMessage.EndPointName, out EndPointProxy))
                    EndPointProxy.OnInvocation(InvMessage);
                else
                {
                    if (Logger.IsWarning()) Logger.Warn($"Received request to end point {InvMessage.EndPointName} but a matching end point was not found");
                }
            }
            else
            {
                Logger.Warn("Unexpected invocation message, will igonore: " + Message.ToString());
            }
        }

        protected override void DisconnectInternal(bool Clean)
        {
            List<TaskCompletionSource<InvocationSuccessReplyMessage>> AllTCS;
            bool lockTaken = false;
            ReplyLock.Enter(ref lockTaken);
            try
            {
                AllTCS = PendingReplies.Values.ToList();
                PendingReplies.Clear();
                OrphanReplies.Clear();
            }
            finally
            {
                ReplyLock.Exit();
            }

            foreach (var TCS in AllTCS)
                TCS.SetCanceled();

            base.DisconnectInternal(Clean);
        }
    }
}
