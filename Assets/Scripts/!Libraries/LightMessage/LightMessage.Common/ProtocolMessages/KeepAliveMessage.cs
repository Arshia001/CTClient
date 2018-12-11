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
    public class KeepAliveMessage : MessageBase
    {
        public static KeepAliveMessage ReadFrom(Message Message)
        {
            return new KeepAliveMessage(Message.AsUInt(1));
        }


        public ulong? NumSecondsUntilNextKeepAlive { get; }


        public KeepAliveMessage(ulong? NumSecondsUntilNextKeepAlive = null)
        {
            this.NumSecondsUntilNextKeepAlive = NumSecondsUntilNextKeepAlive;
        }

        public override MessageType GetMessageType()
        {
            return MessageType.KeepAlive;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return NumSecondsUntilNextKeepAlive.HasValue ? new Param[] { Param.UInt(NumSecondsUntilNextKeepAlive.Value) } : new Param[] { };
        }
    }
}
