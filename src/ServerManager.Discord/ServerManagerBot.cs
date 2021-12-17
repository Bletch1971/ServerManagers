using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerManagerTool.DiscordBot.Delegates;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.DiscordBot.Interfaces;
using ServerManagerTool.DiscordBot.Models;
using ServerManagerTool.DiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot
{
    public sealed class ServerManagerBot : IServerManagerBot
    {
        internal ServerManagerBot()
        {
            Started = false;
        }

        public CancellationToken Token { get; private set; }
        public bool Started { get; private set; }  

        public async Task StartAsync(LogLevel logLevel, string discordToken, string commandPrefix, string dataDirectory, bool allowAllBots, IEnumerable<string> botWhitelist, HandleCommandDelegate handleCommandCallback, HandleTranslationDelegate handleTranslationCallback, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(commandPrefix) || string.IsNullOrWhiteSpace(discordToken) || handleTranslationCallback is null || handleCommandCallback is null)
            {
                return;
            }

            if (Started)
            {
                return;
            }

            Started = true;
            Token = token;

            var settings = new Dictionary<string, string>
            {
                { "DiscordSettings:Token", discordToken },
                { "DiscordSettings:Prefix", commandPrefix },
                { "DiscordSettings:LogLevel", logLevel.ToString() },
                { "ServerManager:DataDirectory", dataDirectory },
            };

            // Begin building the configuration file
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var socketConfig = new DiscordSocketConfig
            {
                LogLevel = LogLevelHelper.GetLogSeverity(logLevel),
                MessageCacheSize = 1000,
            };
            if (Environment.OSVersion.Version < new Version(6, 2))
            {
                // windows 7 or early
                socketConfig.WebSocketProvider = WS4NetProvider.Instance;
            }

            var commandConfig = new CommandServiceConfig
            {
                // Force all commands to run async
                DefaultRunMode = RunMode.Async,
                LogLevel = LogLevelHelper.GetLogSeverity(logLevel),
                CaseSensitiveCommands = false,
            };

            var discordBotConfig = new DiscordBotConfig
            {
                AllowAllBots = allowAllBots,
                DiscordBotWhitelists = new List<DiscordBotWhitelist> ( botWhitelist.Select(i => new DiscordBotWhitelist { BotId = i }) ),
            };

            // Build the service provider
            var services = new ServiceCollection()
                // Add the discord client to the service provider
                .AddSingleton(new DiscordSocketClient(socketConfig))
                // Add the command service to the service provider
                .AddSingleton(new CommandService(commandConfig))
                // Add remaining services to the provider
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<LoggingService>()
                .AddSingleton<StartupService>()
                .AddSingleton<ShutdownService>()
                .AddSingleton<Random>()
                .AddSingleton(config)
                .AddSingleton(discordBotConfig)
                .AddSingleton(handleCommandCallback)
                .AddSingleton(handleTranslationCallback)
                .AddSingleton<IServerManagerBot>(this);

            // Create the service provider
            using (var provider = services.BuildServiceProvider())
            {
                // Initialize the logging service, startup service, and command handler
                provider?.GetRequiredService<LoggingService>();
                await provider?.GetRequiredService<StartupService>().StartAsync();
                provider?.GetRequiredService<CommandHandlerService>();

                try
                {
                    // Prevent the application from closing
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("Task Canceled");
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("Operation Canceled");
                }

                await provider?.GetRequiredService<ShutdownService>().StopAsync();
                Started = false;
            }
        }
    }
}
