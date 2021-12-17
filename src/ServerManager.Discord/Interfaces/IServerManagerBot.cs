using ServerManagerTool.DiscordBot.Delegates;
using ServerManagerTool.DiscordBot.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Interfaces
{
    public interface IServerManagerBot
    {
        CancellationToken Token { get; }

        Task StartAsync(LogLevel logLevel, string discordToken, string commandPrefix, string dataDirectory, bool allowAllBots, IEnumerable<string> botWhitelist, HandleCommandDelegate handleCommandCallback, HandleTranslationDelegate handleTranslationCallback, CancellationToken token);
    }
}
