using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Util
{
    public class NullLogProvider : ILogProvider
    {
        public LogLevel GetLevel()
        {
            return LogLevel.None;
        }

        public void Log(string Text, LogLevel Level)
        {
        }
    }
}
