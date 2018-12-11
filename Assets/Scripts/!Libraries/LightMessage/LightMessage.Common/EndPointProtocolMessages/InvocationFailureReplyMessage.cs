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
    public class InvocationFailureReplyMessage : InvocationReplyMessage
    {
        public static InvocationFailureReplyMessage ReadFrom(Message Message)
        {
            var Id = ReadMessageId(Message);
            var InvocationId = ReadInvocationMessageID(Message);
            var Reason = (Message.Params[3] as ParamString)?.Value;

            if (Id.HasValue && InvocationId.HasValue && Reason != null)
                return new InvocationFailureReplyMessage(Id.Value, InvocationId.Value, Reason, Message.Params.Skip(4));

            return null;
        }


        public string FailureReason { get; }


        public InvocationFailureReplyMessage(ulong InvocationMessageID, string FailureReason, params Param[] Params) : this(0, InvocationMessageID, FailureReason, Params) { }

        public InvocationFailureReplyMessage(ulong InvocationMessageID, string FailureReason, IEnumerable<Param> Params) : this(0, InvocationMessageID, FailureReason, Params) { }

        private InvocationFailureReplyMessage(ulong MessageId, ulong InvocationMessageID, string FailureReason, IEnumerable<Param> Params) : base(MessageId, InvocationMessageID, Params)
        {
            this.FailureReason = FailureReason;
        }

        public override MessageType GetMessageType()
        {
            return MessageType.Invocation_FailureReply;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return base.GetSerializationParams().Concat(new Param[] { Param.String(FailureReason) });
        }
    }
}
