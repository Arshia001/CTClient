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
    public class ReadyMessage : MessageBase
    {
        public static ReadyMessage ReadFrom(Message Message)
        {
            return new ReadyMessage();
        }


        public ReadyMessage() { }

        public override MessageType GetMessageType()
        {
            return MessageType.Ready;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new Param[] { };
        }
    }
}
