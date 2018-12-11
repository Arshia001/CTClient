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
    public class AuthRequestMessage : AuthMessageBase
    {
        public static AuthRequestMessage ReadFrom(Message Message)
        {
            return new AuthRequestMessage(Message.Params.Skip(1));
        }


        public AuthRequestMessage(params Param[] Params) : this(Params.AsEnumerable()) { }

        public AuthRequestMessage(IEnumerable<Param> Params) : base(Params) { }

        public override MessageType GetMessageType()
        {
            return MessageType.AuthRequest;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new Param[] { };
        }
    }
}
