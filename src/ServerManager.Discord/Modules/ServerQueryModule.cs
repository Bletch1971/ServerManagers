using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using ServerManagerTool.Discord.Enums;
using System;
using System.Threading.Tasks;

namespace ServerManagerTool.Discord.Modules
{
    [Name("Server Query")]
    public sealed class ServerQueryModule : InteractiveBase
    {
        private readonly CommandService _service;
        private readonly IConfigurationRoot _config;

        public ServerQueryModule(CommandService service, IConfigurationRoot config)
        {
            _service = service;
            _config = config;
        }

        [Command("info", RunMode = RunMode.Async)]
        [Summary("Poll server for information")]
        [Remarks("info")]
        public async Task ServerInfoAsync()
        {
            await ServerInfoAsync(null);
        }

        [Command("info", RunMode = RunMode.Async)]
        [Summary("Poll server for information")]
        [Remarks("info profileId")]
        public async Task ServerInfoAsync(string profileId)
        {
            try
            {
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = DiscordBot.HandleCommandCallback?.Invoke(CommandType.ServerInfo, channelId, profileId);
                if (response is null || response.Count == 0)
                {
                    await ReplyAsync("No servers associated with this channel.");
                }
                else
                {
                    foreach (var output in response)
                    {
                        await ReplyAsync(output);
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync($"'{Context.Message}' command sent and failed with exception ({ex.Message})");
            }
        }

        [Command("list", RunMode = RunMode.Async)]
        [Summary("List of all servers associated with this channel")]
        [Remarks("list")]
        public async Task ServerListAsync()
        {
            try
            {
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = DiscordBot.HandleCommandCallback?.Invoke(CommandType.ServerList, channelId, null);
                if (response is null || response.Count == 0)
                {
                    await ReplyAsync("No servers associated with this channel.");
                }
                else
                {
                    foreach (var output in response)
                    {
                        await ReplyAsync(output);
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync($"'{Context.Message}' command sent and failed with exception ({ex.Message})");
            }
        }

        [Command("status", RunMode = RunMode.Async)]
        [Summary("Poll server for status")]
        [Remarks("status")]
        public async Task ServerStatusAsync()
        {
            await ServerStatusAsync(null);
        }

        [Command("status", RunMode = RunMode.Async)]
        [Summary("Poll server for status")]
        [Remarks("status profileId")]
        public async Task ServerStatusAsync(string profileId)
        {
            try
            {
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = DiscordBot.HandleCommandCallback?.Invoke(CommandType.ServerStatus, channelId, profileId);
                if (response is null || response.Count == 0)
                {
                    await ReplyAsync("No servers associated with this channel.");
                }
                else
                {
                    foreach (var output in response)
                    {
                        await ReplyAsync(output);
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync($"'{Context.Message}' command sent and failed with exception ({ex.Message})");
            }
        }
    }
}
