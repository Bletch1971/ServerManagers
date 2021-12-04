using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using ServerManagerTool.Discord.Enums;
using System;
using System.Threading.Tasks;

namespace ServerManagerTool.Discord.Modules
{
    [Name("Server Commands")]
    public sealed class ServerCommandModule : InteractiveBase
    {
        private readonly CommandService _service;
        private readonly IConfigurationRoot _config;

        public ServerCommandModule(CommandService service, IConfigurationRoot config)
        {
            _service = service;
            _config = config;
        }

        [Command("backup", RunMode = RunMode.Async)]
        [Summary("Perform a backup of the server")]
        [Remarks("backup")]
        public async Task BackupServerAsync()
        {
            await BackupServerAsync(null);
        }

        [Command("backup", RunMode = RunMode.Async)]
        [Summary("Perform a backup of the server")]
        [Remarks("backup profileId")]
        public async Task BackupServerAsync(string profileId)
        {
            try
            {
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = DiscordBot.HandleCommandCallback?.Invoke(CommandType.BackupServer, channelId, profileId);
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

        [Command("shutdown", RunMode = RunMode.Async)]
        [Summary("Shuts down the server properly")]
        [Remarks("shutdown")]
        public async Task ShutdownServerAsync()
        {
            await ShutdownServerAsync(null);
        }

        [Command("shutdown", RunMode = RunMode.Async)]
        [Summary("Shuts down the server properly")]
        [Remarks("shutdown profileId")]
        public async Task ShutdownServerAsync(string profileId)
        {
            try
            {
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = DiscordBot.HandleCommandCallback?.Invoke(CommandType.ShutdownServer, channelId, profileId);
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

        [Command("start", RunMode = RunMode.Async)]
        [Summary("Starts the server")]
        [Remarks("start")]
        public async Task StartServerAsync()
        {
            await StartServerAsync(null);
        }

        [Command("start", RunMode = RunMode.Async)]
        [Summary("Starts the server")]
        [Remarks("start profileId")]
        public async Task StartServerAsync(string profileId)
        {
            try
            {
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = DiscordBot.HandleCommandCallback?.Invoke(CommandType.StartServer, channelId, profileId);
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

        [Command("stop", RunMode = RunMode.Async)]
        [Summary("Forcibly stops the server")]
        [Remarks("stop")]
        public async Task StopServerAsync()
        {
            await StopServerAsync(null);
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Summary("Forcibly stops the server")]
        [Remarks("stop profileId")]
        public async Task StopServerAsync(string profileId)
        {
            try
            {
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = DiscordBot.HandleCommandCallback?.Invoke(CommandType.StopServer, channelId, profileId);
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

        [Command("update", RunMode = RunMode.Async)]
        [Summary("Updates the server")]
        [Remarks("update")]
        public async Task UpdateServerAsync()
        {
            await UpdateServerAsync(null);
        }

        [Command("update", RunMode = RunMode.Async)]
        [Summary("Updates the server")]
        [Remarks("update profileId")]
        public async Task UpdateServerAsync(string profileId)
        {
            try
            {
                var channelId = Context?.Channel?.Id.ToString() ?? string.Empty;

                var response = DiscordBot.HandleCommandCallback?.Invoke(CommandType.UpdateServer, channelId, profileId);
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
