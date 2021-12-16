using ServerManagerTool.DiscordBot.Delegates;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Interfaces
{
    public interface IServerManagerBot
    {
        CancellationToken Token { get; }

        Task StartAsync(string discordToken, string commandPrefix, string dataDirectory, IEnumerable<string> botWhitelist, HandleCommandDelegate handleCommandCallback, HandleTranslationDelegate handleTranslationCallback, CancellationToken token);
    }
}
