using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using ServerManagerTool.DiscordBot.Delegates;
using ServerManagerTool.DiscordBot.Enums;
using System;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Modules
{
    [Name("Server Commands")]
    public sealed class ServerCommandModule : InteractiveBase
    {
        private readonly CommandService _service;
        private readonly HandleCommandDelegate _handleCommandCallback;
        private readonly IConfigurationRoot _config;

        public ServerCommandModule(CommandService service, HandleCommandDelegate handleCommandCallback, IConfigurationRoot config)
        {
            _service = service;
            _handleCommandCallback = handleCommandCallback;
            _config = config;
        }

        [Command("backup", RunMode = RunMode.Async)]
        [Summary("Perform a backup of the server")]
        [Remarks("backup profileId")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task BackupServerAsync(string profileId)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Backup, serverId, channelId, profileId);
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
        [Remarks("shutdown profileId")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task ShutdownServerAsync(string profileId)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Shutdown, serverId, channelId, profileId);
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
        [Remarks("start profileId")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task StartServerAsync(string profileId)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Start, serverId, channelId, profileId);
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
        [Remarks("stop profileId")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task StopServerAsync(string profileId)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Stop, serverId, channelId, profileId);
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
        [Remarks("update profileId")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task UpdateServerAsync(string profileId)
        {
            try
            {
                var serverId = Context?.Guild?.Id.ToString() ?? string.Empty;
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = _handleCommandCallback?.Invoke(CommandType.Update, serverId, channelId, profileId);
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
