using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Client
{
    static class ClientConfig
    {
        internal static int ReconnectMaxRetries { get; set; } = 8;
        internal static int AuthResponseTimeoutMilliseconds { get; set; } = 10000;
        internal static int DisconnectTimeoutMilliseconds { get; set; } = 10000;
    }
}
