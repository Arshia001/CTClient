using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Util
{
    public class Logger
    {
        ILogProvider LogProvider;
        LogLevel Level;

        public Logger(ILogProvider LogProvider)
        {
            this.LogProvider = LogProvider;
            Level = LogProvider.GetLevel();
        }

        void Log(string Text, LogLevel Level)
        {
            LogProvider.Log($"[{DateTime.Now.ToShortTimeString()}][{Level.ToString().Substring(0, 1)}] {Text}", Level);
        }

        public bool IsVerbose()
        {
            return Level <= LogLevel.Verbose;
        }

        public bool IsInfo()
        {
            return Level <= LogLevel.Info;
        }

        public bool IsWarning()
        {
            return Level <= LogLevel.Warning;
        }

        public bool IsError()
        {
            return Level <= LogLevel.Error;
        }

        public void Verbose(string Text)
        {
            if (IsVerbose())
                Log(Text, LogLevel.Verbose);
        }

        public void Info(string Text)
        {
            if (IsInfo())
                Log(Text, LogLevel.Info);
        }

        public void Warn(string Text)
        {
            if (IsWarning())
                Log(Text, LogLevel.Warning);
        }

        public void Error(string Text)
        {
            if (IsError())
                Log(Text, LogLevel.Error);
        }
    }
}
