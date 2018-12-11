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
    public class RealTimeUnreliableRpcMessage : MessageBase, IRealTimeRpcMessage
    {
        public static RealTimeUnreliableRpcMessage ReadFrom(Message message)
        {
            var objectID = message.AsUInt(1);
            var procedureID = message.AsUInt(2);

            if (objectID.HasValue && procedureID.HasValue)
                return new RealTimeUnreliableRpcMessage(objectID.Value, procedureID.Value, message.Params.Skip(3));

            return null;
        }


        public ulong ObjectID { get; }

        public ulong ProcedureID { get; }

        public bool IsReliable => false;


        public RealTimeUnreliableRpcMessage(ulong objectID, ulong procedureID, IEnumerable<Param> parameters) : base(parameters)
        {
            ObjectID = objectID;
            ProcedureID = procedureID;
        }

        public RealTimeUnreliableRpcMessage(ulong objectID, ulong procedureID, params Param[] parameters) : this(objectID, procedureID, parameters.AsEnumerable()) { }

        public override MessageType GetMessageType()
        {
            return MessageType.RealTimeUnreliableRpc;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new Param[] { Param.UInt(ObjectID), Param.UInt(ProcedureID) };
        }
    }
}
