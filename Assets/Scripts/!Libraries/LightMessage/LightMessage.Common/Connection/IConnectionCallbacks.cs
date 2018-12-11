using LightMessage.Common.ProtocolMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Connection
{
    public interface IConnectionCallbacks
    {
        void OnNewInvocation(InvocationMessageBase Message);
        void TerminateSession();
        void OnDisconnect();
    }
}
