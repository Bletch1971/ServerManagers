using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.DiscordBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Services
{
    public class CommandHandlerService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly LoggingService _logger;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;
        private readonly DiscordBotWhitelistConfig _botWhitelist;

        public CommandHandlerService(DiscordSocketClient discord, CommandService commands, LoggingService logger, IConfigurationRoot config, IServiceProvider provider, DiscordBotWhitelistConfig botWhitelist)
        {
            _discord = discord;
            _commands = commands;
            _logger = logger;
            _config = config;
            _provider = provider;
            _botWhitelist = botWhitelist ?? new DiscordBotWhitelistConfig();
            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (LogLevel.Debug.ToString().Equals(_config["DiscordSettings:LogLevel"]))
            {
                await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Intercepted the following message from {s.Author.Username} ({s.Author.Id}) - {s.Content}"));
            }

            // Ensure the message is from a user/bot
            if (!(s is SocketUserMessage msg))
            {
                return;
            }

            // Ignore self when checking commands
            if (msg.Author == _discord.CurrentUser)
            {
                return;
            }

            // check if the author is a bot
            if (msg.Author.IsBot)
            {
                // check if bot is on the whitelist
                if (!_botWhitelist.DiscordBotWhitelists.Any(b => b.BotId.Equals(msg.Author.Id.ToString())))
                {
                    // Tell bot to ignore
                    return;
                }
            }

            // Check if the message has a valid command prefix
            var argPos = 0;
            if (msg.HasStringPrefix(_config["DiscordSettings:Prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                // Create the command context
                var context = new SocketCommandContext(_discord, msg);

                // Execute the command
                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                if (!result.IsSuccess)
                {
                    // If not successful, reply with the error.
                    await context.Channel.SendMessageAsync(result.ToString());
                }
            }
        }
    }
}