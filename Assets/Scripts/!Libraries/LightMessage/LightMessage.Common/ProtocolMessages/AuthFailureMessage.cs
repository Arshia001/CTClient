using LightMessage;
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
    public class AuthFailureMessage : AuthMessageBase
    {
        public static AuthFailureMessage ReadFrom(Message Message)
        {
            return new AuthFailureMessage();
        }


        public AuthFailureMessage() { }

        public override MessageType GetMessageType()
        {
            return MessageType.AuthFailure;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new Param[] { };
        }
    }
}
