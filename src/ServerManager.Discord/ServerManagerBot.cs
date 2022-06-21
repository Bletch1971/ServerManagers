using Discord.Commands;
using Discord.Interactions;
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
using System.Diagnostics;
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

        public async Task RunAsync(DiscordBotConfig discordBotConfig, HandleCommandDelegate handleCommandCallback, HandleTranslationDelegate handleTranslationCallback, CancellationToken token)
        {
            if (discordBotConfig is null || handleTranslationCallback is null || handleCommandCallback is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(discordBotConfig.DiscordToken))
            {
                throw new Exception("#DiscordBot_MissingTokenError");
            }

            if (string.IsNullOrWhiteSpace(discordBotConfig.CommandPrefix))
            {
                throw new Exception("#DiscordBot_MissingPrefixError");
            }

            if (Started)
            {
                return;
            }

            Started = true;
            Token = token;

            // Begin building the configuration file
            var config = new ConfigurationBuilder()
                .Build();

            var socketConfig = new DiscordSocketConfig
            {
                LogLevel = LogLevelHelper.GetLogSeverity(discordBotConfig.LogLevel),
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
                DefaultRunMode = Discord.Commands.RunMode.Async,
                LogLevel = LogLevelHelper.GetLogSeverity(discordBotConfig.LogLevel),
                CaseSensitiveCommands = false,
            };

            var interactionConfig = new InteractionServiceConfig
            {
                // Force all interactions to run async
                DefaultRunMode = Discord.Interactions.RunMode.Async,
                LogLevel = LogLevelHelper.GetLogSeverity(discordBotConfig.LogLevel),
            };

            // Build the service provider
            var services = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton(discordBotConfig)
                // Add the discord client to the service provider
                .AddSingleton(new DiscordSocketClient(socketConfig))
                // Add the command service to the service provider
                .AddSingleton(new CommandService(commandConfig))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), interactionConfig))
                // Add remaining services to the provider
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<LoggingService>()
                .AddSingleton<StartupService>()
                .AddSingleton<ShutdownService>()
                .AddSingleton<Random>()
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
