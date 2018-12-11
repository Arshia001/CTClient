using LightMessage.Common.Messages;
using LightMessage.Common.ProtocolMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Connection
{
    public class InvocationFailureException : Exception
    {
        public string FailureReason { get; }
        public IReadOnlyList<Param> Params { get; }


        public InvocationFailureException(InvocationFailureReplyMessage FailureMessage) : base(FailureMessage.FailureReason)
        {
            this.FailureReason = FailureMessage.FailureReason;
            this.Params = FailureMessage.Params;
        }
    }
}
