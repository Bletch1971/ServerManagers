using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.DiscordBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Services
{
    public class CommandHandlerService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly LoggingService _logger;
        private readonly IServiceProvider _services;
        private readonly DiscordBotConfig _botConfig;

        public CommandHandlerService(DiscordSocketClient client, CommandService commands, LoggingService logger, IServiceProvider services, DiscordBotConfig botConfig)
        {
            _client = client;
            _commands = commands;
            _logger = logger;
            _services = services;
            _botConfig = botConfig ?? new DiscordBotConfig();

            _commands.CommandExecuted += OnCommandExecutedAsync;
            _client.MessageReceived += OnMessageReceivedAsync;
        }

        public async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            var commandName = command.IsSpecified ? command.Value.Name : "A command";

            // We can tell the user what went wrong
            if (!string.IsNullOrWhiteSpace(result?.ErrorReason))
            {
                switch (result?.Error)
                {
                    case CommandError.BadArgCount:
                        await context.Channel.SendMessageAsync("Parameter count does not match any command.");
                        break;
                    default:
                        await context.Channel.SendMessageAsync(result.ErrorReason);
                        break;
                }
            }

            if (LogLevelHelper.CheckLogLevel(LogLevel.Info, _botConfig.LogLevel))
            {
                await _logger?.OnLogAsync(new LogMessage(LogSeverity.Info, "CommandExecution", $"{commandName} was executed at {DateTime.Now}."));
            }
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (LogLevelHelper.CheckLogLevel(LogLevel.Debug, _botConfig.LogLevel))
                await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Intercepted the following message from {s.Author.Username} ({s.Author.Id}) - {s.Content}"));

            // Ensure the message is a valid user socket message
            if (!(s is SocketUserMessage msg))
                return;

            // Ignore self
            if (msg.Author.Id == _client.CurrentUser.Id)
            {
                if (LogLevelHelper.CheckLogLevel(LogLevel.Debug, _botConfig.LogLevel))
                    await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message has come from this bot, message will be ignored."));

                return;
            }

            // check if the author is a bot
            if (msg.Author.IsBot)
            {
                if (_botConfig.AllowAllBots)
                {
                    if (LogLevelHelper.CheckLogLevel(LogLevel.Debug, _botConfig.LogLevel))
                        await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message has come from another bot, allow all bots enabled."));
                }
                else
                {
                    if (LogLevelHelper.CheckLogLevel(LogLevel.Debug, _botConfig.LogLevel))
                        await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message has come from another bot, checking if bot is in the whitelist."));

                    if (!_botConfig.DiscordBotWhitelist.Any(botId => botId.Equals(msg.Author.Id.ToString())))
                    {
                        if (LogLevelHelper.CheckLogLevel(LogLevel.Debug, _botConfig.LogLevel))
                            await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message has come from another bot, bot is not in the whitelist, message will be ignored."));

                        return;
                    }
                }
            }

            // Check if the message has a valid command prefix
            var argPos = 0;
            if (msg.HasStringPrefix(_botConfig.CommandPrefix, ref argPos, StringComparison.OrdinalIgnoreCase) || msg.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                if (LogLevelHelper.CheckLogLevel(LogLevel.Debug, _botConfig.LogLevel))
                    await _logger?.OnLogAsync(new LogMessage(LogSeverity.Debug, MessageSource.System.ToString(), $"Message prefix matched, message will be processed."));

                // Create the command context
                var context = new SocketCommandContext(_client, msg);

                // Execute the command
                await _commands.ExecuteAsync(context, argPos, _services);
            }
        }
    }
}