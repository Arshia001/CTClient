using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Util
{
    public class ConsoleLogProvider : ILogProvider
    {
        LogLevel Level;


        public ConsoleLogProvider(LogLevel Level)
        {
            this.Level = Level;
        }

        public LogLevel GetLevel()
        {
            return Level;
        }

        public void Log(string Text, LogLevel Level)
        {
            Console.ForegroundColor = GetColor(Level);
            Console.WriteLine(Text);
        }

        ConsoleColor GetColor(LogLevel Level)
        {
            switch (Level)
            {
                case LogLevel.Verbose:
                    return ConsoleColor.Gray;
                case LogLevel.Info:
                    return ConsoleColor.Green;
                case LogLevel.Warning:
                    return ConsoleColor.Yellow;
                case LogLevel.Error:
                    return ConsoleColor.Red;
                default:
                    return ConsoleColor.Magenta;
            }
        }
    }
}
