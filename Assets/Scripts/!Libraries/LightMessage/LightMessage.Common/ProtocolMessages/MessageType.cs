using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.ProtocolMessages
{
    public enum MessageType
    {
        KeepAlive = 0,
        Invocation = 1,
        Ack = 2,
        Invocation_SuccessReply = 3,
        Invocation_FailureReply = 4,
        RealTimeReliableRpc = 5,
        RealTimeUnreliableRpc = 6,
        Custom = 7,

        // The codes below add an extra byte to the message since values are variable-length encoded starting with 4 bits of space
        AuthRequest = 8,
        AuthResponse = 9,
        AuthRejoin = 10,
        AuthFailure = 11,
        Ready = 12,
        CleanDisconnect = 13
    }
}
