using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ServerManagerTool.DiscordBot.Models;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly DiscordBotConfig _botConfig;

        public StartupService(DiscordSocketClient client, CommandService commands, IServiceProvider services, DiscordBotConfig botConfig)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _botConfig = botConfig;
        }

        public async Task StartAsync()
        {
            // Get the discord token from the config file
            var discordToken = _botConfig?.DiscordToken;

            if (string.IsNullOrWhiteSpace(discordToken))
            {
                throw new Exception("#DiscordBot_MissingTokenError");
            }

            // Load commands and modules into the command service
            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

            // Login to discord
            await _client.LoginAsync(TokenType.Bot, discordToken);
            // Connect to the websocket
            await _client.StartAsync();
        }
    }
}