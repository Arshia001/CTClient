using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRClient
{
    public static class Extensions
    {
        // The compiler keeps generating warnings (CS4014) whenever a Task is unused and 
        // not awaited. Calling this will prevent the warning from being generated.
        public static void DontCare(this Task Task) { }
    }
}