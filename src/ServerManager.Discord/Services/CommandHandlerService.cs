using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;
        private readonly DiscordBotWhitelistConfig _botWhitelist;

        public CommandHandlerService(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider, DiscordBotWhitelistConfig botWhitelist)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;
            _botWhitelist = botWhitelist ?? new DiscordBotWhitelistConfig();
            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            // Ensure the message is from a user/bot
            var msg = s as SocketUserMessage;
            if (msg is null)
            {
                return;
            }

            // Ignore self when checking commands
            if (msg.Author == _discord.CurrentUser)
            {
                return;
            }

            // Tell bot to ignore itself, unless on the whitelist
            if (msg.Author.IsBot && !_botWhitelist.DiscordBotWhitelists.Any(b => b.BotId.Equals(msg.Author.Id)))
            {
                return;
            }

            // Create the command context
            var context = new SocketCommandContext(_discord, msg);

            // Check if the message has a valid command prefix
            var argPos = 0;
            if (msg.HasStringPrefix(_config["DiscordSettings:Prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
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