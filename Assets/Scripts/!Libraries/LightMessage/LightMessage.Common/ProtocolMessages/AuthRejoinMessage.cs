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
    public class AuthRejoinMessage : AuthMessageBase
    {
        public static AuthRejoinMessage ReadFrom(Message Message)
        {
            var SessionId = (Message.Params[1] as ParamGuid)?.Value;

            if (SessionId.HasValue)
                return new AuthRejoinMessage(SessionId.Value);

            return null;
        }


        public Guid SessionId { get; }


        public AuthRejoinMessage(Guid SessionId)
        {
            this.SessionId = SessionId;
        }

        public override MessageType GetMessageType()
        {
            return MessageType.AuthRejoin;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new SingleEnumerable<Param>(Param.Guid(SessionId));
        }
    }
}
