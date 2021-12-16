using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Services
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        private string LogDirectory { get; }
        private string LogFile => Path.Combine(LogDirectory, $"ServerManager_DiscordBot.{DateTime.Now:yyyyMMdd}.log");

        public LoggingService(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config)
        {
            _discord = discord;
            _commands = commands;
            _config = config;

            // Get the data directory from the config file
            var rootDirectory = _config["ServerManager:DataDirectory"] ?? AppContext.BaseDirectory;
            LogDirectory = Path.Combine(rootDirectory, "logs");

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        internal async Task OnLogAsync(LogMessage message)
        {
            // Create the log directory if it doesn't exist
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            // Create today's log file if it doesn't exist
            if (!File.Exists(LogFile))
            {
                File.Create(LogFile).Dispose();
            }

            var logText = $"{DateTime.Now:HH:mm:ss:ffff} [{message.Severity}] {message.Source}: {message.Exception?.ToString() ?? message.Message}";

            // Write the log text to a file
            File.AppendAllText(LogFile, logText + "\n");

            // Write the log text to the console
            await Console.Out.WriteLineAsync(logText);
        }
    }
}