using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Util
{
    public enum LogLevel
    {
        Verbose,
        Info,
        Warning,
        Error,
        None
    }

    public interface ILogProvider
    {
        void Log(string Text, LogLevel Level);
        LogLevel GetLevel();
    }
}
