using LightMessage.Common.Messages;
using LightMessage.Common.ProtocolMessages;
using LightMessage.Common.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightMessage.Client.EndPoints
{
    public class EndPointProxy
    {
        EndPointClient Client;
        Logger Logger;

        public string EndPointName { get; }
        ConcurrentDictionary<string, Action<IReadOnlyList<Param>>> Functions = new ConcurrentDictionary<string, Action<IReadOnlyList<Param>>>();


        internal EndPointProxy(EndPointClient Client, string Name, Logger Logger)
        {
            EndPointName = Name;
            this.Client = Client;
            this.Logger = Logger;
        }

        public async Task<IReadOnlyList<Param>> SendInvocationForReply(string Function, CancellationToken CancellationToken, params Param[] Params)
        {
            return (await Client.SendInvocationForReply(new InvocationMessage(EndPointName, Function, Params), CancellationToken)).Params;
        }

        public async Task<IReadOnlyList<Param>> SendInvocationForReply(string Function, CancellationToken CancellationToken, IEnumerable<Param> Params)
        {
            return (await Client.SendInvocationForReply(new InvocationMessage(EndPointName, Function, Params), CancellationToken)).Params;
        }

        public Task SendInvocation(string Function, CancellationToken CancellationToken, params Param[] Params)
        {
            return Client.SendInvocation(new InvocationMessage(EndPointName, Function, Params), CancellationToken);
        }

        public Task SendInvocation(string Function, CancellationToken CancellationToken, IEnumerable<Param> Params)
        {
            return Client.SendInvocation(new InvocationMessage(EndPointName, Function, Params), CancellationToken);
        }

        public Task Register()
        {
            return Client.Register(this);
        }

        public Task Unregister()
        {
            return Client.Unregister(this);
        }

        public void On(string Function, Action<IReadOnlyList<Param>> Handler)
        {
            if (!Functions.TryAdd(Function, Handler))
                throw new InvalidOperationException("Cannot register handler more than once");
        }

        internal void OnInvocation(InvocationMessage InvMessage)
        {
            Action<IReadOnlyList<Param>> Delegate;
            if (Functions.TryGetValue(InvMessage.FunctionName, out Delegate))
                Delegate(InvMessage.Params);
            else
            {
                if (Logger.IsWarning()) Logger.Warn($"Received request for function {InvMessage.FunctionName} in end point {InvMessage.EndPointName} but a matching function handler was not found");
            }
        }
    }
}
