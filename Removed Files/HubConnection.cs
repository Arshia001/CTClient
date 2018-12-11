//#define FORCE_LOG_MESSAGES
#define LOG_ERRORS

#if UNITY_EDITOR || FORCE_LOG_MESSAGES
#define LOG_MESSAGES
#endif


using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SignalRClient
{
    //?? must test disconnection scenarios, for now all this was tested with is a VERY good connection
    public class HubConnection : IDisposable
    {
        public delegate void ResultCallback(JSONNode Result, string ErrorMessage, JSONNode ErrorDetails);


        public enum EState
        {
            New,
            Connected,
            Connecting,
            Disconnected
        }


        string ServiceEndpointUrlHttp;
        string ServiceEndpointUrlWs;

        ClientWebSocket WebSocket = new ClientWebSocket();
        string ConnectionToken;
        string ConnectionData;
        string GroupsToken;
        string LastMessageId;
        int KeepAliveTimeout;

        int NextMessageId = 1;

        public EState State { get; private set; } = EState.New;
        object LockObj = new object();

        DateTime KeepAliveTime;
        CancellationTokenSource KeepAliveCTS;

        System.Random ReconnectTimeoutRandom = new System.Random();
        CancellationTokenSource ReconnectCTS;

        bool bDisposed = false;
        bool bConnectedOnce = false;
        CancellationTokenSource ReceiveCTS;

        List<string> UnsentMessages = new List<string>();
        ConcurrentDictionary<int, ResultCallback> ResultCallbacks = new ConcurrentDictionary<int, ResultCallback>();
        ConcurrentDictionary<string, HubProxy> Hubs = new ConcurrentDictionary<string, HubProxy>();

        TaskCompletionSource<bool> WaitForServerWelcomeTCS;


        void LogMessage(string Message)
        {
#if LOG_MESSAGES
            Debug.Log($"[SignalR Client][{DateTime.Now}] {Message}");
#endif
        }

        internal void LogError(string Message)
        {
#if LOG_ERRORS
            Debug.LogError($"[SignalR Client][{DateTime.Now}] {Message}");
#endif
        }

        JSONNode CreateFlatJSON(Dictionary<string, object> Values)
        {
            var Result = new JSONClass();
            foreach (var KV in Values)
                Result.Add(KV.Key, JSON.FromData(KV.Value));

            return Result;
        }

        JSONNode CreateJSONArray(params JSONNode[] Nodes)
        {
            var Result = new JSONArray();
            foreach (var N in Nodes)
                Result.Add(N);
            return Result;
        }

        public HubConnection(string ServiceEndpointUrl)
        {
            ServiceEndpointUrlHttp = "http://" + ServiceEndpointUrl.TrimEnd('/') + '/';
            ServiceEndpointUrlWs = "ws://" + ServiceEndpointUrl.TrimEnd('/') + '/';
        }

        public HubProxy CreateProxy(string HubName)
        {
            if (State != EState.New)
                throw new InvalidOperationException("Cannot create proxies after connection");

            HubName = HubName.ToLower();
            if (Hubs.ContainsKey(HubName))
                return Hubs[HubName];

            var Hub = new HubProxy(this, HubName);
            if (!Hubs.TryAdd(HubName, Hub))
                throw new Exception("Failed to add proxy");

            return Hub;
        }

        public async Task Connect(CancellationToken CancellationToken)
        {
            State = EState.Connecting;

            try
            {
                ConnectionData = CreateJSONArray(Hubs.Select(kv => CreateFlatJSON(new Dictionary<string, object>() { { "name", kv.Key } })).ToArray()).Serialize();

                LogMessage("Connecting to " + ServiceEndpointUrlHttp);

                var NegotiateResponse = JSON.Parse(await ServerUtility.HttpGetAsync(new Uri(new Uri(ServiceEndpointUrlHttp), "negotiate"), new Dictionary<string, object>
                {
                    { "connectionData", ConnectionData },
                    { "clientProtocol", "1.3" }
                }));

                LogMessage(NegotiateResponse.Serialize());

                if (!NegotiateResponse["TryWebSockets"].AsBool.GetValueOrDefault())
                    throw new InvalidOperationException("Endpoint does not support websockets");

                ConnectionToken = NegotiateResponse["ConnectionToken"].AsString;
                KeepAliveTimeout = NegotiateResponse["KeepAliveTimeout"].AsInt.GetValueOrDefault();

                WaitForServerWelcomeTCS = new TaskCompletionSource<bool>();
                await Task.WhenAll(RecreateSocket(CancellationToken), WaitForServerWelcomeTCS.Task);
            }
            catch
            {
                State = EState.Disconnected;
                throw;
            }
        }

        async void KeepAlive(CancellationToken CancellationToken)
        {
            while (true)
            {
                if (CancellationToken.IsCancellationRequested)
                    return;

                await Task.Delay(KeepAliveTime - DateTime.Now);

                if ((KeepAliveTime - DateTime.Now).TotalSeconds >= 1) // KeepAlive was changed while we slept
                    continue;

                KeepAliveTime = DateTime.Now.AddSeconds(KeepAliveTimeout);

                if (State == EState.Connected)
                {
                    LogMessage("Keep-alive timed out, attempting to reconnect to server");
                    Reconnect();
                }
            }
        }

        public void Disconnect()
        {
            State = EState.Disconnected;

            if (ReceiveCTS != null)
                ReceiveCTS.Cancel();


            if (ReconnectCTS != null)
            {
                ReconnectCTS.Cancel();
                ReconnectCTS = null;
            }

            if (KeepAliveCTS != null)
            {
                KeepAliveCTS.Cancel();
                KeepAliveCTS = null;
            }

            WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).DontCare();

            ServerUtility.HttpGetAsync(new Uri(new Uri(ServiceEndpointUrlHttp), "abort"), new Dictionary<string, object>
            {
                { "transport", "webSockets" },
                { "connectionToken", ConnectionToken },
                { "connectionData", ConnectionData },
            }).DontCare();
        }

        protected virtual void Dispose(bool bDisposing)
        {
            if (!bDisposed)
            {
                Disconnect();

                bDisposed = true;
            }
        }

        ~HubConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsConnected()
        {
            return WebSocket != null && WebSocket.State == WebSocketState.Open && State == EState.Connected;
        }

        void Reconnect()
        {
            if (ReconnectCTS != null)
            {
                ReconnectCTS.Cancel();
                ReconnectCTS = null;
            }

            if (State != EState.Connected && State != EState.Connecting)
                return;

            RecreateSocket(CancellationToken.None).DontCare();
        }

        void ReconnectDelay()
        {
            if (ReconnectCTS != null || State != EState.Connected)
                return;

            ReconnectCTS = new CancellationTokenSource();
            ReconnectDelay_Impl(ReconnectCTS.Token);
        }

        async void ReconnectDelay_Impl(CancellationToken CancellationToken)
        {
            State = EState.Connecting;

            await Task.Delay(ReconnectTimeoutRandom.Next(2000, 4000));

            if (CancellationToken.IsCancellationRequested)
                return;

            Reconnect();
        }

        async Task RecreateSocket(CancellationToken CancellationToken)
        {
            LogMessage("Attempting to connect");

            Uri Uri;
            if (bConnectedOnce)
            {
                var QueryString = new Dictionary<string, object>
                {
                    { "transport", "webSockets" },
                    { "connectionToken", ConnectionToken },
                    { "connectionData", ConnectionData },
                };

                if (!string.IsNullOrEmpty(LastMessageId))
                    QueryString.Add("messageId", LastMessageId);
                if (!string.IsNullOrEmpty(GroupsToken))
                    QueryString.Add("groupsToken", GroupsToken);

                Uri = ServerUtility.CreateUri(new Uri(new Uri(ServiceEndpointUrlWs), "reconnect"), QueryString);
            }
            else
                Uri = ServerUtility.CreateUri(new Uri(new Uri(ServiceEndpointUrlWs), "connect"), new Dictionary<string, object>
                {
                    { "transport", "webSockets" },
                    { "connectionToken", ConnectionToken },
                    { "connectionData", ConnectionData },
                });

            try
            {
                await WebSocket.ConnectAsync(Uri, CancellationToken);
                LogMessage("Opened connection");

                if (ReceiveCTS != null)
                    ReceiveCTS.Cancel();

                ReceiveCTS = new CancellationTokenSource();
                ReceiveAsync(ReceiveCTS.Token).DontCare(); //?? store reference to task, otherwise we get multiple receive tasks

                KeepAliveTime = DateTime.Now.AddSeconds(KeepAliveTimeout);
                if (KeepAliveCTS == null)
                {
                    KeepAliveCTS = new CancellationTokenSource();
                    KeepAlive(KeepAliveCTS.Token);
                }

                while (UnsentMessages.Count > 0)
                {
                    var M = UnsentMessages[0];
                    await SendRawMessage(M, false, CancellationToken);
                    UnsentMessages.RemoveAt(0);
                }
            }
            catch
            {
                ReconnectDelay();
            }
        }

        async Task ReceiveAsync(CancellationToken CancellationToken)
        {
            LogMessage("Starting receiver");

            const int CHUNK_SIZE = 1024;
            ArraySegment<byte> Buffer = new ArraySegment<byte>(new byte[CHUNK_SIZE]);
            MemoryStream Stream = new MemoryStream(CHUNK_SIZE); // Will expand as needed, but it shouldn't happen frequently

            while (true)
            {
                try
                {
                    if (CancellationToken.IsCancellationRequested)
                        return;

                    if (WebSocket.State != WebSocketState.Open)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    LogMessage("Wait for next message");

                    var Result = await WebSocket.ReceiveAsync(Buffer, CancellationToken);

                    if (Result.CloseStatus != null)
                        WebSocket_OnClose(Result.CloseStatus.Value, Result.CloseStatusDescription);
                    else if (Result.MessageType == WebSocketMessageType.Text)
                    {
                        Stream.Write(Buffer.Array, 0, Result.Count);
                        if (Result.EndOfMessage)
                        {
                            var Data = Encoding.UTF8.GetString(Stream.ToArray());
                            WebSocket_OnMessage(Data);
                            Stream.SetLength(0);
                        }
                    }
                }
                catch (Exception Ex)
                {
                    WebSocket_OnError(Ex);
                }
            }
        }

        void WebSocket_OnMessage(string Data)
        {
            try
            {
                LogMessage(Data);

                var Json = JSON.Parse(Data);
                if (Json.IsEmpty)
                {
                    KeepAliveTime = DateTime.Now.AddSeconds(KeepAliveTimeout);
                }
                else
                {
                    LastMessageId = Json["C"].AsString;
                    GroupsToken = Json["G"].AsString;

                    if (Json["S"] != null && Json["S"].AsInt == 1) //?? note: this fails to arrive in rare cases. should set a timeout for the message and reconnect if it fails
                    {
                        State = EState.Connected;
                        WaitForServerWelcomeTCS.SetResult(true);
                        bConnectedOnce = true;
                        LogMessage("Connected successfully");
                    }

                    var MessageArray = Json["M"];
                    if (!MessageArray.IsEmpty)
                    {
                        for (int Idx = 0; Idx < MessageArray.Count; ++Idx)
                        {
                            var HubName = MessageArray[Idx]["H"];
                            var Method = MessageArray[Idx]["M"];
                            var Params = MessageArray[Idx]["A"];

                            HubProxy Proxy;
                            if (HubName == null || !Hubs.TryGetValue(HubName.AsString.ToLower(), out Proxy))
                                continue;

                            Proxy.OnMessage(Method.AsString.ToLower(), Params.AsArray);
                        }
                    }

                    int MessageIdx;
                    if (Json["I"] != null)
                    {
                        MessageIdx = Json["I"].AsInt.GetValueOrDefault();

                        ResultCallback Cb;
                        if (ResultCallbacks.TryRemove(MessageIdx, out Cb))
                        {
                            try
                            {
                                if (Json["E"] != null)
                                {
                                    if (Json["H"] != null && Json["H"].AsBool == true && Json["D"] != null)
                                        Cb(null, Json["E"].AsString, Json["D"]);
                                    else
                                        Cb(null, Json["E"].AsString, null);
                                }
                                else if (Json["R"] != null)
                                    Cb(Json["R"], null, null);
                                else
                                    Cb(null, null, null); // void method result
                            }
                            catch (Exception Ex)
                            {
                                LogError($"Error in result callback for message ID {MessageIdx}: {Ex}");
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                LogError("OnMessage error: " + Ex.ToString());
            }
        }

        void WebSocket_OnError(Exception Ex)
        {
            LogError(Ex.ToString());

            if ((State == EState.Connected || State == EState.Connecting) && WebSocket.State != WebSocketState.Connecting && WebSocket.State != WebSocketState.Open)
                ReconnectDelay();
        }

        void WebSocket_OnClose(WebSocketCloseStatus CloseStatus, string CloseStatusDescription)
        {
            if (CloseStatus != WebSocketCloseStatus.NormalClosure && (State == EState.Connected || State == EState.Connecting))
                ReconnectDelay();

            LogMessage($"Connection closed with status: {CloseStatus}, description: {CloseStatusDescription ?? "none provided"}");
        }

        // Registers callback and returns allocated ID of message.
        internal int RegisterCallback(ResultCallback ResultCallback)
        {
            int MessageId;
            lock (LockObj)
            {
                MessageId = NextMessageId++;
            }

            if (ResultCallback != null)
                if (!ResultCallbacks.TryAdd(MessageId, ResultCallback))
                    throw new Exception("Failed to register callback");

            return MessageId;
        }

        internal Task SendMessage(string Message, CancellationToken CancellationToken)
        {
            LogMessage(Message);

            return SendRawMessage(Message, true, CancellationToken);
        }

        async Task SendRawMessage(string Message, bool HandleErrors, CancellationToken CancellationToken)
        {
            try
            {
                // We leave the handling of reconnection/disconnection/etc. to the websocket.
                var MessageBytes = Encoding.UTF8.GetBytes(Message);
                await WebSocket.SendAsync(new ArraySegment<byte>(MessageBytes), WebSocketMessageType.Text, true, CancellationToken);
            }
            catch (Exception Ex)
            {
                if (HandleErrors)
                {
                    UnsentMessages.Add(Message);
                    WebSocket_OnError(Ex);
                }
                else
                    throw;
            }
        }
    }
}
