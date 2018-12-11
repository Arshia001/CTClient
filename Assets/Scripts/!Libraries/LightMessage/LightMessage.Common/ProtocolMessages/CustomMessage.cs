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
    // Can be used together with the unreliable connection only
    public class CustomMessage : MessageBase
    {
        public static CustomMessage ReadFrom(Message Message)
        {
            return new CustomMessage(Message.Params.Skip(1));
        }


        public CustomMessage(IEnumerable<Param> parameters) : base(parameters) { }

        public CustomMessage(params Param[] parameters) : this(parameters.AsEnumerable()) { }

        public override MessageType GetMessageType()
        {
            return MessageType.Custom;
        }

        protected override IEnumerable<Param> GetSerializationParams()
        {
            return new Param[] { };
        }
    }
}
