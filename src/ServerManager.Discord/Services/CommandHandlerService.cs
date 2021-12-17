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
        private readonly DiscordBotConfig _botConfig;

        public CommandHandlerService(DiscordSocketClient discord, CommandService commands, LoggingService logger, IConfigurationRoot config, IServiceProvider provider, DiscordBotConfig botConfig)
        {
            _discord = discord;
            _commands = commands;
            _logger = logger;
            _config = config;
            _provider = provider;
            _botConfig = botConfig ?? new DiscordBotConfig();
            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (LogLevel.Debug.ToString().Equals(_config["DiscordSettings:LogLevel"]))
                await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Intercepted the following message from {s.Author.Username} ({s.Author.Id}) - {s.Content}"));

            // Ensure the message is a valid user socket message
            if (!(s is SocketUserMessage msg))
                return;

            // Ignore self
            if (msg.Author.Id == _discord.CurrentUser.Id)
            {
                if (LogLevel.Debug.ToString().Equals(_config["DiscordSettings:LogLevel"]))
                    await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message has come from this bot, message will be ignored."));

                return;
            }

            // check if the author is a bot
            if (msg.Author.IsBot)
            if (_botConfig.AllowAllBots)
                {
                    if (LogLevel.Debug.ToString().Equals(_config["DiscordSettings:LogLevel"]))
                        await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message has come from another bot, allow all bots enabled."));
                }
                else
                {
                    if (LogLevel.Debug.ToString().Equals(_config["DiscordSettings:LogLevel"]))
                        await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message has come from another bot, checking if bot is in the whitelist."));

                    if (!_botConfig.DiscordBotWhitelists.Any(b => b.BotId.Equals(msg.Author.Id.ToString())))
                    {
                        if (LogLevel.Debug.ToString().Equals(_config["DiscordSettings:LogLevel"]))
                            await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message has come from another bot, bot is not in the whitelist, message will be ignored."));

                        return;
                    }
                } 

            // Check if the message has a valid command prefix
            var argPos = 0;
            if (msg.HasStringPrefix(_config["DiscordSettings:Prefix"], ref argPos, StringComparison.OrdinalIgnoreCase) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                if (LogLevel.Debug.ToString().Equals(_config["DiscordSettings:LogLevel"]))
                    await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message prefix matched, message will be processed."));

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