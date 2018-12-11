using LightMessage.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.ProtocolMessages
{
#if !UNITY
    [Orleans.Concurrency.Immutable]
#endif
    public class InvocationMessage : InvocationMessageBase
    {
        public static InvocationMessage ReadFrom(Message Message)
        {
            var Id = ReadMessageId(Message);
            var EndPointName = (Message.Params[2] as ParamString)?.Value;
            var FunctionName = (Message.Params[3] as ParamString)?.Value;

            if (Id.HasValue && EndPointName != null && FunctionName != null)
                return new InvocationMessage(Id.Value, EndPointName, FunctionName, Message.Params.Skip(4));

            return null;

        }


        public string EndPointName { get; }
        public string FunctionName { get; }


        public InvocationMessage(string EndPointName, string FunctionName, params Param[] Params) : this(0, EndPointName, FunctionName, Params) { }

        public InvocationMessage(string EndPointName, string FunctionName, IEnumerable<Param> Params) : this(0, EndPointName, FunctionName, Params) { }

        private InvocationMessage(ulong MessageID, string EndPointName, string FunctionName, IEnumerable<Param> Params) : base(MessageID, Params)
        {
            this.EndPointName = EndPointName;
            this.FunctionName = FunctionName;
        }

        public override MessageType GetMessageType()
        {
            return MessageType.Invocation;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return base.GetSerializationParams().Concat(new Param[] { Param.String(EndPointName), Param.String(FunctionName) });
        }
    }
}
