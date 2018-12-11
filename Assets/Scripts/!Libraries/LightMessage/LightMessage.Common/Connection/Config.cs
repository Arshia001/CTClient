using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Connection
{
    public static class Config
    {
        public static int MessageMaxSize { get; set; } = 65536;
        public static int MessageMaxLengthSectionBytes { get; set; } = 2;
        public static int RequestMaxCacheEntries { get; set; } = 32;
        public static int RequestMaxConcurrentActive { get; set; } = 32;
    }
}
