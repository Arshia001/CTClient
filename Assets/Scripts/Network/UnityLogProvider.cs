using LightMessage.Common.Util;
using UnityEngine;

namespace Network
{
    public class UnityLogProvider : ILogProvider
    {
        LogLevel Level;

        public UnityLogProvider(LogLevel Level)
        {
            this.Level = Level;
        }

        public LogLevel GetLevel()
        {
            return Level;
        }

        public void Log(string Text, LogLevel Level)
        {
            switch (Level)
            {
                case LogLevel.Verbose:
                case LogLevel.Info:
                    Debug.Log(Text);
                    break;

                case LogLevel.Warning:
                    Debug.LogWarning(Text);
                    break;

                case LogLevel.Error:
                    Debug.LogError(Text);
                    break;
            }
        }
    }
}