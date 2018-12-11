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
    public class InvocationSuccessReplyMessage : InvocationReplyMessage
    {
        public static InvocationSuccessReplyMessage ReadFrom(Message Message)
        {
            var Id = ReadMessageId(Message);
            var InvocationId = ReadInvocationMessageID(Message);

            if (Id.HasValue && InvocationId.HasValue)
                return new InvocationSuccessReplyMessage(Id.Value, InvocationId.Value, Message.Params.Skip(3));

            return null;
        }


        public InvocationSuccessReplyMessage(ulong InvocationMessageID, params Param[] Params) : this(0, InvocationMessageID, Params) { }

        public InvocationSuccessReplyMessage(ulong InvocationMessageID, IEnumerable<Param> Params) : this(0, InvocationMessageID, Params) { }

        private InvocationSuccessReplyMessage(ulong MessageId, ulong InvocationMessageID, IEnumerable<Param> Params) : base(MessageId, InvocationMessageID, Params) { }

        public override MessageType GetMessageType()
        {
            return MessageType.Invocation_SuccessReply;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return base.GetSerializationParams();
        }
    }
}
