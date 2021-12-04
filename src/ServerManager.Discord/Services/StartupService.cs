using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ServerManagerTool.Discord.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        public StartupService(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;
        }

        public async Task StartAsync()
        {
            // Get the discord token from the config file
            var discordToken = _config["DiscordSettings:Token"];

            if (string.IsNullOrWhiteSpace(discordToken))
            {
                throw new Exception("#DiscordBot_MissingTokenError");
            }

            // Login to discord
            await _discord.LoginAsync(TokenType.Bot, discordToken);
            // Connect to the websocket
            await _discord.StartAsync();

            // Load commands and modules into the command service
            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _provider);
        }
    }
}