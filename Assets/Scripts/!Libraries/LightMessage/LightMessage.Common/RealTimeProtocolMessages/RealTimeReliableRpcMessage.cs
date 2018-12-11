using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightMessage.Common.Messages;

namespace LightMessage.Common.ProtocolMessages
{
#if !UNITY
    [Orleans.Concurrency.Immutable]
#endif
    public class RealTimeReliableRpcMessage : InvocationMessageBase, IRealTimeRpcMessage
    {
        public static RealTimeReliableRpcMessage ReadFrom(Message message)
        {
            var messageID = ReadMessageId(message);
            var objectID = message.AsUInt(2);
            var procedureID = message.AsUInt(3);

            if (messageID.HasValue && objectID.HasValue && procedureID.HasValue)
                return new RealTimeReliableRpcMessage(messageID.Value, objectID.Value, procedureID.Value, message.Params.Skip(4));

            return null;
        }


        public ulong ObjectID { get; }

        public ulong ProcedureID { get; }

        public bool IsReliable => true;


        public RealTimeReliableRpcMessage(ulong messageID, ulong objectID, ulong procedureID, IEnumerable<Param> parameters) : base(messageID, parameters)
        {
            ObjectID = objectID;
            ProcedureID = procedureID;
        }

        public RealTimeReliableRpcMessage(ulong messageID, ulong objectID, ulong procedureID, params Param[] parameters) : this(messageID, objectID, procedureID, parameters.AsEnumerable()) { }

        public override MessageType GetMessageType()
        {
            return MessageType.RealTimeReliableRpc;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return base.GetSerializationParams().Concat(new Param[] { Param.UInt(ObjectID), Param.UInt(ProcedureID) });
        }
    }
}
