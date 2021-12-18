using Discord;
using System;

namespace ServerManagerTool.DiscordBot.Enums
{
    public enum LogLevel
    {
        Critical = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Verbose = 4,
        Debug = 5
    }

    public class LogLevelHelper
    {
        public static LogSeverity GetLogSeverity(LogLevel logLevel)
        {
            if (Enum.TryParse(logLevel.ToString(), out LogSeverity logSeverity))
                return logSeverity;
            return LogSeverity.Info;
        }

        public static bool CheckLogLevel(LogLevel LogLevelToCheck, LogLevel configLogLevel)
        {
            return (int)configLogLevel >= (int)LogLevelToCheck;
        }
    }
}
