using Discord.WebSocket;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Services
{
    public class ShutdownService
    {
        private readonly DiscordSocketClient _client;

        public ShutdownService(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task StopAsync()
        {
            await _client.StopAsync();
            await _client.LogoutAsync();
        }
    }
}
