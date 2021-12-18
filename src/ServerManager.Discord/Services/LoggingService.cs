using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NLog;
using NLog.Config;
using NLog.Targets;
using ServerManagerTool.DiscordBot.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Services
{
    public class LoggingService
    {
        private readonly Logger _logger;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly DiscordBotConfig _botConfig;

        public LoggingService(DiscordSocketClient client, CommandService commands, DiscordBotConfig botConfig)
        {
            _client = client;
            _commands = commands;
            _botConfig = botConfig;

            var logFilePath = Path.Combine(_botConfig.DataDirectory ?? AppContext.BaseDirectory, "logs");

            _logger = GetLogger(logFilePath, "", "ServerManager_DiscordBot", LogLevel.Debug, LogLevel.Fatal, _botConfig.MaxArchiveFiles, _botConfig.MaxArchiveDays);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff} [INFO] Logging Enabled: {LogManager.IsLoggingEnabled()}");

            _client.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        internal async Task OnLogAsync(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    _logger?.Fatal($"{message.Source}: {message.Exception?.ToString() ?? message.Message}");
                    break;
                case LogSeverity.Error:
                    _logger?.Error($"{message.Source}: {message.Exception?.ToString() ?? message.Message}");
                    break;
                case LogSeverity.Warning:
                    _logger?.Warn($"{message.Source}: {message.Exception?.ToString() ?? message.Message}");
                    break;
                case LogSeverity.Info:
                    _logger?.Info($"{message.Source}: {message.Exception?.ToString() ?? message.Message}");
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    _logger?.Debug($"{message.Source}: {message.Exception?.ToString() ?? message.Message}");
                    break;
            }

            // Write the log text to the console
            await Console.Out.WriteLineAsync($"{DateTime.Now:HH:mm:ss.ffff} [{message.Severity}] {message.Source}: {message.Exception?.ToString() ?? message.Message}");
        }

        private static Logger GetLogger(string logFilePath, string logType, string logName, LogLevel minLevel, LogLevel maxLevel, int maxArchiveFiles, int maxArchiveDays)
        {
            if (string.IsNullOrWhiteSpace(logFilePath) || string.IsNullOrWhiteSpace(logName))
                return null;

            var loggerName = $"{logType ?? string.Empty}_{logName}".Replace(" ", "_");

            if (LogManager.Configuration.FindTargetByName(loggerName) is null)
            {
                var logFile = new FileTarget(loggerName)
                {
                    FileName = Path.Combine(logFilePath, $"{logName}.log"),                    
                    Layout = "${time} [${level:uppercase=true}] ${message}",
                    ArchiveFileName = Path.Combine(logFilePath, $"{logName}.{{#}}.log"),
                    ArchiveNumbering = ArchiveNumberingMode.Date,
                    ArchiveEvery = FileArchivePeriod.Day,
                    ArchiveDateFormat = "yyyyMMdd",
                    ArchiveOldFileOnStartup = true,
                    MaxArchiveFiles = maxArchiveFiles,
                    MaxArchiveDays = maxArchiveDays,
                    CreateDirs = true,
                };
                LogManager.Configuration.AddTarget(loggerName, logFile);

                var rule = new LoggingRule(loggerName, minLevel, maxLevel, logFile);
                LogManager.Configuration.LoggingRules.Add(rule);
                LogManager.ReconfigExistingLoggers();
            }

            return LogManager.GetLogger(loggerName);
        }
    }
}