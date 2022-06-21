using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ServerManagerTool.DiscordBot.Delegates;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.DiscordBot.Interfaces;

namespace ServerManagerTool.DiscordBot.Modules
{
    [Name("Server Query")]
    public sealed class ServerQueryModule : ModuleBase<SocketCommandContext>
    {
        private const int COMMAND_RESPONSE_DELAY = 500;

        private readonly IServerManagerBot _serverManagerBot;
        private readonly HandleCommandDelegate _handleCommandCallback;

        public ServerQueryModule(IServerManagerBot serverManagerBot, HandleCommandDelegate handleCommandCallback)
        {
            _serverManagerBot = serverManagerBot;
            _handleCommandCallback = handleCommandCallback;
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
                        if (output is null)
                            continue;
                        await ReplyAsync(output.Replace("&", "_"));
                        await Task.Delay(COMMAND_RESPONSE_DELAY);
                    }

                    await ReplyAsync($"'{Context.Message}' command complete.");
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
                        if (output is null)
                            continue;
                        await ReplyAsync(output.Replace("&", "_"));
                        await Task.Delay(COMMAND_RESPONSE_DELAY);
                    }

                    await ReplyAsync($"'{Context.Message}' command complete.");
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync($"'{Context.Message}' command sent and failed with exception ({ex.Message})");
            }
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
                        if (output is null)
                            continue;
                        await ReplyAsync(output.Replace("&", "_"));
                        await Task.Delay(COMMAND_RESPONSE_DELAY);
                    }

                    await ReplyAsync($"'{Context.Message}' command complete.");
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync($"'{Context.Message}' command sent and failed with exception ({ex.Message})");
            }
        }
    }
}
