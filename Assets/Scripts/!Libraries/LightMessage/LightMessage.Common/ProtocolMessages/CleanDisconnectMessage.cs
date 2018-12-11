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
    class CleanDisconnectMessage : MessageBase
    {
        public static CleanDisconnectMessage ReadFrom(Message Message)
        {
            return new CleanDisconnectMessage();
        }


        public CleanDisconnectMessage() { }

        public override MessageType GetMessageType()
        {
            return MessageType.CleanDisconnect;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new Param[0];
        }
    }
}
