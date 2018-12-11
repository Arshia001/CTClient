using LightMessage.Common.Messages;
using LightMessage.Common.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightMessage.Common.ProtocolMessages
{
#if !UNITY
    [Orleans.Concurrency.Immutable]
#endif
    public abstract class InvocationMessageBase : MessageBase
    {
        protected static ulong? ReadMessageId(Message Message)
        {
            return (Message.Params[1] as ParamUInt)?.Value;
        }


        public ulong ID { get; internal set; } // This is set only by ReliableConnection, and has adverse effects on (semantic) immutability


        protected InvocationMessageBase(ulong MessageID, IEnumerable<Param> Params) : base(Params)
        {
            ID = MessageID;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new SingleEnumerable<Param>(Param.UInt(ID));
        }
    }
}
