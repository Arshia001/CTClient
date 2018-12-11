using LightMessage.Client.EndPoints;
using LightMessage.Common.Connection;
using LightMessage.Common.Messages;
using LightMessage.Common.ProtocolMessages;
using LightMessage.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
    public class ConnectionManager : MonoBehaviour
    {
        public static ConnectionManager Instance { get; private set; }


        //static IPEndPoint ParseIPEndpoint(string IPEndPoint)
        //{
        //    int Len = IPEndPoint.LastIndexOf(':');
        //    return new IPEndPoint(
        //        IPAddress.Parse(IPEndPoint.Substring(0, Len)),
        //        Convert.ToInt32(IPEndPoint.Substring(Len + 1)));
        //}


        public string ServerHostName = "ct.sperlous.com";
        public string[] ServerIPs;
        public int ServerPort = 1020;
        public bool UseLocalServer = false;


        Dictionary<Type, IEndPointHandler> HandlerCache = new Dictionary<Type, IEndPointHandler>();


        public EndPointClient Client { get; private set; }

        public bool IsConnected => Client != null && Client.IsConnected;
        public bool IsConnectedOrReconnecting => Client != null && Client.IsConnectedOrReconnecting;


        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Client = new EndPointClient(new UnityLogProvider(
#if DEBUG
            LogLevel.Verbose
#else
            LogLevel.Info
#endif
            ));

            foreach (var H in GetComponents<IEndPointHandler>())
            {
                H.SetupHub(this);
                HandlerCache[H.GetType()] = H;
            }

            Client.OnSessionTerminated += Client_OnSessionTerminated;
        }

        public async Task Connect()
        {
            //?? Google play sign-in
            AuthRequestMessage AuthMessage;
            if (PersistentData.Instance.ClientId.HasValue)
                AuthMessage = new AuthRequestMessage(Param.Guid(PersistentData.Instance.ClientId.Value));
            else
                AuthMessage = new AuthRequestMessage();

            try
            {
                IPAddress ServerIP;
#if UNITY_EDITOR
                if (UseLocalServer)
                {
                    ServerIP = IPAddress.Parse("127.0.0.1");
                }
                else
                {
#endif
                    try
                    {
                        Debug.Log("Resloving server host name");
                        if (string.IsNullOrEmpty(ServerHostName))
                            throw new Exception("No host name provided");
                        var AllIPs = await Dns.GetHostEntryAsync(ServerHostName);
                        Debug.Log($"DNS result: {AllIPs.AddressList.Aggregate("", (acc, ip) => acc + ip.ToString() + ", ")}");
                        ServerIP = AllIPs.AddressList[UnityEngine.Random.Range(0, AllIPs.AddressList.Length)];
                        Debug.Log($"Selected IP: {ServerIP}");
                    }
                    catch (Exception Ex)
                    {
                        Debug.LogError($"Failed to resolve {ServerHostName} due to {Ex}, will choose out of build-time IPs instead");
                        ServerIP = IPAddress.Parse(ServerIPs[UnityEngine.Random.Range(0, ServerIPs.Length)]);
                    }
#if UNITY_EDITOR
                }
#endif

                await Client.Connect(new IPEndPoint(ServerIP, ServerPort), CancellationToken.None, AuthMessage, true);

                PersistentData.Instance.ClientId = Client.SessionId;
                Debug.Log($"Signed in, ID is {PersistentData.Instance.ClientId}");
            }
            catch (AuthenticationFailedException)
            {
                Debug.LogError($"Failed to sign in, clearing saved ID and retrying");
                PersistentData.Instance.ClientId = null;
                await Connect();
            }
        }

        public void Disconnect()
        {
            if (Client != null)
                Client.OnSessionTerminated -= Client_OnSessionTerminated;

            Client?.Disconnect();
        }

        void Client_OnSessionTerminated(bool WasCleanShutdown)
        {
            Debug.LogWarning("Session terminated, will check and load startup scene");
            if (SceneManager.sceneCount > 1 || SceneManager.GetSceneAt(0).name != "Startup")
                SceneManager.LoadScene("Startup");
        }

        void OnDestroy()
        {
            Disconnect();
        }

        public T EndPoint<T>() where T : class, IEndPointHandler
        {
            IEndPointHandler Result;
            return HandlerCache.TryGetValue(typeof(T), out Result) ? (T)Result : null;
        }

        public void DelayKeepAlive(TimeSpan Delay)
        {
            Client.DelayKeepAlive(Delay);
        }
    }
}
