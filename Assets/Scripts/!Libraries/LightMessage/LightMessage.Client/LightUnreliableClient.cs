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
    public class LightUnreliableClient
    {
        public bool IsAuthenticated => authMessage != null;

        public delegate void OnMessageDelegate(MessageBase message);
        public event OnMessageDelegate OnMessage;

        internal Logger Logger { get; private set; }

        UdpClient udpClient;
        IPEndPoint serverEndPoint;
        bool connecting = false;

        CancellationTokenSource ListenCancellationTokenSource;

        AuthRequestMessage authMessage;


        public LightUnreliableClient(ILogProvider LogProvider = null)
        {
            Logger = new Logger(LogProvider ?? new NullLogProvider());
        }

        public async Task Connect(IPEndPoint IPEndPoint, CancellationToken CancellationToken, AuthRequestMessage AuthMessage)
        {
            Logger.Info($"Starting unreliable client for server address {IPEndPoint}");

            if (authMessage != null || connecting)
            {
                Logger.Error("Client already connecting or previously connected");
                throw new InvalidOperationException("Already connected or connecting, can't connect again");
            }

            try
            {
                connecting = true;

                udpClient = new UdpClient();
                serverEndPoint = IPEndPoint;

                udpClient.Connect(serverEndPoint);

                var receiveTask = udpClient.ReceiveAsync();

                while (true) //?? maximum retry count
                {
                    if (Logger.IsVerbose()) Logger.Verbose($"UDP sending authentication message to server");

                    await AuthMessage.Send(udpClient);

                    await (Task.WhenAny(receiveTask, Task.Delay(1000))); //?? time parameter

                    if (receiveTask.IsCompleted)
                        break;
                    else if (receiveTask.IsFaulted)
                        throw receiveTask.Exception;
                    else if (receiveTask.IsCanceled)
                        throw new TaskCanceledException();
                }

                var authResult = MessageBase.ReadFrom(new ArraySegment<byte>(receiveTask.Result.Buffer));

                switch (authResult.GetMessageType())
                {
                    case MessageType.AuthResponse:
                        break;

                    case MessageType.AuthFailure:
                        if (Logger.IsError()) Logger.Error("Server refused to authenticate with provided credentials");
                        throw new AuthenticationFailedException();

                    default:
                        var ErrorDesc = $"The server responded with a message of type {authResult.GetMessageType()} while we were expecting an authentication response.";
                        if (Logger.IsError()) Logger.Error(ErrorDesc);
                        throw new InvalidOperationException(ErrorDesc);
                }

                var authResponseMessage = authResult as AuthResponseMessage;
                if (Logger.IsVerbose()) Logger.Verbose($"Received authentication response: {authResponseMessage}");
                this.authMessage = AuthMessage;

                Listen();

                Logger.Info($"UDP handshake completed");
            }
            finally
            {
                connecting = false;
            }

        }

        public void Disconnect()
        {
            authMessage = null;
            ListenCancellationTokenSource?.Cancel();
            udpClient.Dispose();
            udpClient = null;
        }

        public void SendMessage(MessageBase Message, CancellationToken CancellationToken)
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Cannot send messages before authentication");

            if (Logger.IsVerbose()) Logger.Verbose($"Sending invocation to server: {Message}");
            Message.Send(udpClient);
        }

        async void Listen()
        {
            ListenCancellationTokenSource = new CancellationTokenSource();

            var receiveTask = udpClient.ReceiveAsync();

            try
            {
                while (true)
                {
                    if (ListenCancellationTokenSource.IsCancellationRequested)
                        return;

                    await (Task.WhenAny(receiveTask, Task.Delay(1000))); //?? time parameter

                    if (receiveTask.IsCompleted || receiveTask.IsFaulted || receiveTask.IsCanceled)
                    {
                        if (receiveTask.IsCompleted)
                        {
                            MessageBase message;

                            try
                            {
                                message = MessageBase.ReadFrom(new ArraySegment<byte>(receiveTask.Result.Buffer));

                                if (Logger.IsInfo()) Logger.Info($"Received message {message}");

                                switch (message.GetMessageType())
                                {
                                    case MessageType.AuthResponse:
                                        break;

                                    case MessageType.AuthFailure:
                                        authMessage?.Send(udpClient).DontCare();
                                        break;

                                    default:
                                        OnMessage?.Invoke(message);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (Logger.IsInfo()) Logger.Info($"Invalid message {Convert.ToBase64String(receiveTask.Result.Buffer)} : {ex}");
                            }
                        }

                        receiveTask = udpClient.ReceiveAsync();
                    }
                }
            }
            catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is ObjectDisposedException))
            {
                // connection terminated, nothing else to do
            }
            catch (ObjectDisposedException)
            {
                // connection terminated, nothing else to do
            }
            catch (Exception ex)
            {
                Logger.Error($"UDP client failed to listen due to exception: {ex}");
                Disconnect();
            }
        }
    }
}
