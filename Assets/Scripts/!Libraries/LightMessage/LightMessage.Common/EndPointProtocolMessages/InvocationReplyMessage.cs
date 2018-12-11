using LightMessage.Common.Messages;
using LightMessage.Common.Util;
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
    public abstract class InvocationReplyMessage : InvocationMessageBase
    {
        protected static ulong? ReadInvocationMessageID(Message Message)
        {
            return (Message.Params[2] as ParamUInt)?.Value;
        }


        public ulong InvocationMessageID { get; }


        protected InvocationReplyMessage(ulong MessageID, ulong InvocationMessageID, IEnumerable<Param> Params) : base(MessageID, Params)
        {
            this.InvocationMessageID = InvocationMessageID;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return base.GetSerializationParams().Concat(new SingleEnumerable<Param>(Param.UInt(InvocationMessageID)));
        }
    }
}
