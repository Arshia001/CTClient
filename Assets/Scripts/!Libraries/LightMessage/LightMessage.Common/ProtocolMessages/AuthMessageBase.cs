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
    public abstract class AuthMessageBase : MessageBase
    {
        protected AuthMessageBase() : base() { }
        protected AuthMessageBase(IEnumerable<Param> Params) : base(Params) { }
    }
}
