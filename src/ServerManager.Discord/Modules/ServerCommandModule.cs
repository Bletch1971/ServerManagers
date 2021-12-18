using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using ServerManagerTool.DiscordBot.Delegates;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.DiscordBot.Interfaces;
using System;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Modules
{
    [Name("Server Commands")]
    public sealed class ServerCommandModule : InteractiveBase
    {
        private readonly IServerManagerBot _serverManagerBot;
        private readonly CommandService _commands;
        private readonly HandleCommandDelegate _handleCommandCallback;

        public ServerCommandModule(IServerManagerBot serverManagerBot, CommandService commands, HandleCommandDelegate handleCommandCallback)
        {
            _serverManagerBot = serverManagerBot;
            _commands = commands;
            _handleCommandCallback = handleCommandCallback;
        }

        [Command("backup", RunMode = RunMode.Async)]
        [Summary("Backup the server")]
        [Remarks("backup profileId|alias")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task BackupServerAsync(string profileIdOrAlias)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Backup, serverId, channelId, profileIdOrAlias, _serverManagerBot.Token);
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

        [Command("restart", RunMode = RunMode.Async)]
        [Summary("Restart the server")]
        [Remarks("restart profileId|alias")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task RestartServerAsync(string profileIdOrAlias)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Restart, serverId, channelId, profileIdOrAlias, _serverManagerBot.Token);
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

        [Command("shutdown", RunMode = RunMode.Async)]
        [Summary("Shuts down the server properly")]
        [Remarks("shutdown profileId|alias")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task ShutdownServerAsync(string profileIdOrAlias)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Shutdown, serverId, channelId, profileIdOrAlias, _serverManagerBot.Token);
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

        [Command("start", RunMode = RunMode.Async)]
        [Summary("Starts the server")]
        [Remarks("start profileId|alias")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task StartServerAsync(string profileIdOrAlias)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Start, serverId, channelId, profileIdOrAlias, _serverManagerBot.Token);
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

        [Command("stop", RunMode = RunMode.Async)]
        [Summary("Forcibly stops the server")]
        [Remarks("stop profileId|alias")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task StopServerAsync(string profileIdOrAlias)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Stop, serverId, channelId, profileIdOrAlias, _serverManagerBot.Token);
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

        [Command("update", RunMode = RunMode.Async)]
        [Summary("Updates the server")]
        [Remarks("update profileId|alias")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task UpdateServerAsync(string profileIdOrAlias)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Update, serverId, channelId, profileIdOrAlias, _serverManagerBot.Token);
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
