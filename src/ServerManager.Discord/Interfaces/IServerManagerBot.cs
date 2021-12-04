using ServerManagerTool.Discord.Delegates;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.Discord.Interfaces
{
    public interface IServerManagerBot
    {
        Task StartAsync(string commandPrefix, string discordToken, string dataDirectory, HandleCommandDelegate handleCommandCallback, CancellationToken token);
    }
}
