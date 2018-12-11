using LightMessage.Common.Messages;
using LightMessage.Common.ProtocolMessages;
using LightMessage.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightMessage.Client
{
    public class LightRealTimeClient
    {
        public bool IsConnected => (reliableClient?.IsConnected ?? false) && (unreliableClient?.IsAuthenticated ?? false);

        public bool IsConnectedOrReconnecting => reliableClient?.IsConnectedOrReconnecting ?? false;


        public delegate void ClientOnAnyInvocationDelegate(IRealTimeRpcMessage message);
        public event ClientOnAnyInvocationDelegate OnMessage;

        public delegate void OnSessionTerminatedDelegate(bool wasCleanShutdown);
        public event OnSessionTerminatedDelegate OnSessionTerminated;


        LightClient reliableClient;
        LightUnreliableClient unreliableClient;

        bool bConnecting = false;


        internal Logger Logger { get; private set; }

        public LightRealTimeClient(ILogProvider logProvider = null)
        {
            Logger = new Logger(logProvider ?? new NullLogProvider());

            reliableClient = new LightClient(logProvider);
            reliableClient.OnAnyInvocation += ReliableClient_OnInvocation;
            reliableClient.OnSessionTerminated += ReliableClient_OnSessionTerminated;

            unreliableClient = new LightUnreliableClient(logProvider);
            unreliableClient.OnMessage += UnreliableClient_OnMessage;
        }

        private void UnreliableClient_OnMessage(MessageBase message)
        {
            if (message.GetMessageType() == MessageType.RealTimeUnreliableRpc)
                OnMessage?.Invoke(message as IRealTimeRpcMessage);
            else if (Logger.IsWarning())
                Logger.Warn($"Message is not an RPC message and will be ignored: {message}");
        }

        private void ReliableClient_OnSessionTerminated(bool wasCleanShutdown)
        {
            OnSessionTerminated?.Invoke(wasCleanShutdown);
        }

        private void ReliableClient_OnInvocation(InvocationMessageBase message)
        {
            if (message.GetMessageType() == MessageType.RealTimeReliableRpc)
                OnMessage?.Invoke(message as IRealTimeRpcMessage);
            else if (Logger.IsWarning())
                Logger.Warn($"Message is not an RPC message and will be ignored: {message}");
        }

        public async Task Connect(IPAddress ipAddress, int reliablePort, int unreliablePort, CancellationToken cancellationToken, AuthRequestMessage authMessage)
        {
            Logger.Info($"Starting real-time client for server address {ipAddress}({reliablePort}, {unreliablePort})");

            if (bConnecting)
            {
                Logger.Error("Client already connecting or previously connected");
                throw new InvalidOperationException("Already connected or connecting, can't connect again");
            }

            try
            {
                bConnecting = true;

                await unreliableClient.Connect(new IPEndPoint(ipAddress, unreliablePort), cancellationToken, authMessage);
                await reliableClient.Connect(new IPEndPoint(ipAddress, reliablePort), cancellationToken, authMessage);
            }
            finally
            {
                bConnecting = false;
            }
        }

        public void SendRpc(ulong objectID, ulong procedureID, IEnumerable<Param> parameters, bool sendAsReliable, CancellationToken cancellationToken)
        {
            if (sendAsReliable)
                reliableClient.SendInvocation(new RealTimeReliableRpcMessage(0, objectID, procedureID, parameters), cancellationToken).DontCare();
            else
                unreliableClient.SendMessage(new RealTimeUnreliableRpcMessage(objectID, procedureID, parameters), cancellationToken);
        }

        public void Disconnect()
        {
            reliableClient.Disconnect();
            unreliableClient.Disconnect();
        }
    }
}
