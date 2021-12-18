using Discord;
using Discord.Commands;
using ServerManagerTool.DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Modules
{
    [Name("Help")]
    public sealed class HelpModule : ModuleBase<SocketCommandContext>
    {
        private const int MAX_VALUE_LENGTH = 1024;

        private readonly CommandService _commands;
        private readonly DiscordBotConfig _botConfig;
        private readonly IServiceProvider _services;

        public HelpModule(CommandService commands, IServiceProvider services, DiscordBotConfig botConfig)
        {
            _commands = commands;
            _services = services;
            _botConfig = botConfig;
        }

        [Command("help")]
        [Summary("Provides a list of available commands")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task HelpAsync()
        {
            var prefix = _botConfig.CommandPrefix;

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use"
            };

            foreach (var module in _commands.Modules)
            {
                var moduleName = module.Name;

                // create the list of accessible commands
                var commands = new List<string>(module.Commands.Count);

                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context, _services);
                    if (!result.IsSuccess)
                    {
                        continue;
                    }

                    commands.Add($"{prefix}{cmd.Aliases.First()}");
                }

                // remove all duplicate commands
                commands = commands.Distinct().ToList();

                var commandString = string.Empty;

                foreach (var command in commands)
                {
                    if (string.IsNullOrWhiteSpace(command))
                    {
                        continue;
                    }

                    var commandToAdd = $"{command}\n";

                    if (commandString.Length + commandToAdd.Length > MAX_VALUE_LENGTH)
                    {
                        // force the output, string would be too long
                        builder.AddField(x =>
                        {
                            x.Name = moduleName;
                            x.Value = $"{commandString}\n";
                            x.IsInline = false;
                        });

                        // reset the module name and command string
                        moduleName = $"{module.Name} cont.";
                        commandString = string.Empty;
                    }

                    commandString += commandToAdd;
                }

                if (!string.IsNullOrWhiteSpace(commandString))
                {
                    builder.AddField(x =>
                    {
                        x.Name = moduleName;
                        x.Value = $"{commandString}\n";
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync(string.Empty, false, builder.Build());
        }

        [Command("help")]
        [Summary("Searches a list of available commands")]
        [RequireBotPermission(ChannelPermission.ViewChannel | ChannelPermission.SendMessages)]
        public async Task HelpAsync(string command)
        {
            var searchResults = _commands.Search(Context, command);

            if (!searchResults.IsSuccess)
            {
                await ReplyAsync($"Sorry, couldn't find a command like **{command}**.");
                return;
            }

            var prefix = _botConfig.CommandPrefix;

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in searchResults.Commands)
            {
                var cmd = match.Command;

                var result = await cmd.CheckPreconditionsAsync(Context, _services);
                if (!result.IsSuccess)
                {
                    continue;
                }

                var usage = $"{prefix}{cmd.Aliases.First()}";
                if (cmd.Parameters.Count > 0)
                {
                    usage += $" {string.Join(" ", cmd.Parameters.Select(p => p.Name))}";
                }
                usage += $"\n";

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Summary: {cmd.Summary}\nUsage: {usage}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync(string.Empty, false, builder.Build());
        }
    }
}