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
    public class AckMessage : MessageBase
    {
        public static AckMessage ReadFrom(Message Message)
        {
            var InvocationId = (Message.Params[1] as ParamUInt)?.Value;

            if (InvocationId.HasValue)
                return new AckMessage(InvocationId.Value);

            return null;
        }


        public ulong InvocationId { get; }


        public AckMessage(ulong InvocationId)
        {
            this.InvocationId = InvocationId;
        }

        public override MessageType GetMessageType()
        {
            return MessageType.Ack;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new SingleEnumerable<Param>(Param.UInt(InvocationId));
        }
    }
}
