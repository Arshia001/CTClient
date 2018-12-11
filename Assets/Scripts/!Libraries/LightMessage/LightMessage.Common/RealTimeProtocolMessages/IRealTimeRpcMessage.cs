using LightMessage.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.ProtocolMessages
{
    public interface IRealTimeRpcMessage
    {
        ulong ObjectID { get; }
        ulong ProcedureID { get; }
        IReadOnlyList<Param> Params { get; }
        bool IsReliable { get; }
    }
}
