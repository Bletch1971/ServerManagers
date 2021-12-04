using ServerManagerTool.Discord.Delegates;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.Discord.Interfaces
{
    public interface IServerManagerBot
    {
        Task StartAsync(string discordToken, string commandPrefix, string dataDirectory, HandleCommandDelegate handleCommandCallback, CancellationToken token);
    }
}
