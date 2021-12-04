using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace ServerManagerTool.Discord.Services
{
    public class CommandHandlerService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        public CommandHandlerService(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;

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

            //Tell bot to ignore itself.
            if (msg.Author.IsBot)
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