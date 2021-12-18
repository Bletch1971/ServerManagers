using ServerManagerTool.DiscordBot.Delegates;
using ServerManagerTool.DiscordBot.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Interfaces
{
    public interface IServerManagerBot
    {
        CancellationToken Token { get; }

        Task RunAsync(DiscordBotConfig discordBotConfig, HandleCommandDelegate handleCommandCallback, HandleTranslationDelegate handleTranslationCallback, CancellationToken token);
    }
}
