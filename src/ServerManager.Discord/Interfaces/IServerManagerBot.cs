using ServerManagerTool.DiscordBot.Delegates;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Interfaces
{
    public interface IServerManagerBot
    {
        CancellationToken Token { get; }

        Task StartAsync(string discordToken, string commandPrefix, string dataDirectory, HandleCommandDelegate handleCommandCallback, HandleTranslationDelegate handleTranslationCallback, CancellationToken token);
    }
}
