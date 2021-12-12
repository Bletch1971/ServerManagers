using Discord.WebSocket;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot.Services
{
    public class ShutdownService
    {
        private readonly DiscordSocketClient _discord;

        public ShutdownService(DiscordSocketClient discord)
        {
            _discord = discord;
        }

        public async Task StopAsync()
        {
            await _discord.StopAsync();
            await _discord.LogoutAsync();
        }
    }
}
