using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using ServerManagerTool.DiscordBot.Delegates;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.DiscordBot.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Modules
{
    [Name("Server Query")]
    public sealed class ServerQueryModule : InteractiveBase
    {
        private readonly IServerManagerBot _serverManagerBot;
        private readonly CommandService _service;
        private readonly HandleCommandDelegate _handleCommandCallback;
        private readonly IConfigurationRoot _config;

        public ServerQueryModule(IServerManagerBot serverManagerBot, CommandService service, HandleCommandDelegate handleCommandCallback, IConfigurationRoot config)
        {
            _serverManagerBot = serverManagerBot;
            _service = service;
            _handleCommandCallback = handleCommandCallback;
            _config = config;
        }

        [Command("info", RunMode = RunMode.Async)]
        [Summary("Poll server for information")]
        [Remarks("info")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task ServerInfoAsync()
        {
            await ServerInfoAsync(null);
        }

        [Command("info", RunMode = RunMode.Async)]
        [Summary("Poll server for information")]
        [Remarks("info profileId|alias")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task ServerInfoAsync(string profileIdOrAlias)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Info, serverId, channelId, profileIdOrAlias, _serverManagerBot.Token);
                if (response is null)
                {
                    await ReplyAsync("No servers associated with this channel.");
                }
                else
                {
                    foreach (var output in response)
                    {
                        await ReplyAsync(output.Replace("&", "_"));
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
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task ServerListAsync()
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.List, serverId, channelId, null, _serverManagerBot.Token);
                if (response is null)
                {
                    await ReplyAsync("No servers associated with this channel.");
                }
                else
                {
                    foreach (var output in response)
                    {
                        await ReplyAsync(output.Replace("&", "_"));
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
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task ServerStatusAsync()
        {
            await ServerStatusAsync(null);
        }

        [Command("status", RunMode = RunMode.Async)]
        [Summary("Poll server for status")]
        [Remarks("status profileId|alias")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task ServerStatusAsync(string profileIdOrAlias)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Status, serverId, channelId, profileIdOrAlias, _serverManagerBot.Token);
                if (response is null)
                {
                    await ReplyAsync("No servers associated with this channel.");
                }
                else
                {
                    foreach (var output in response)
                    {
                        await ReplyAsync(output.Replace("&", "_"));
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
