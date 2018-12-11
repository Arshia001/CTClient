using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRClient
{
    public class HubProxy
    {
        HubConnection Connection;
        string HubName;

        Dictionary<string, Action<JSONArray>> ActionMap = new Dictionary<string, Action<JSONArray>>();


        internal HubProxy(HubConnection Connection, string HubName)
        {
            this.Connection = Connection;
            this.HubName = HubName;
        }

        // This will work even in a disconnected state, it'll queue the message for later and send it upon reconnection
        public Task Invoke(string MethodName, CancellationToken CancellationToken, HubConnection.ResultCallback ResultCallback, params object[] Params)
        {
            var ParamsArray = new JSONArray();
            if (Params != null)
                foreach (var P in Params)
                    ParamsArray.Add(JSON.FromData(P)); //?? this only works for flat objects

            var MessageJson = new JSONClass();
            MessageJson.Add("H", new JSONString(HubName));
            MessageJson.Add("I", new JSONString(Connection.RegisterCallback(ResultCallback).ToString()));
            MessageJson.Add("M", new JSONString(MethodName));
            MessageJson.Add("A", ParamsArray);

            var Message = MessageJson.Serialize();

            return Connection.SendMessage(Message, CancellationToken);
        }

        public void On(string Method, Action<JSONArray> Callback)
        {
            ActionMap.Add(Method.ToLower(), Callback);
        }

        internal void OnMessage(string Method, JSONArray Params)
        {
            Action<JSONArray> A;

            if (ActionMap.TryGetValue(Method, out A))
            {
                try
                {
                    A(Params);
                }
                catch (Exception Ex)
                {
                    Connection.LogError($"Error in handler for method {Method}: {Ex}");
                }
            }
        }
    }
}