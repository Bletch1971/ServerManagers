using System.Collections.Generic;

namespace ServerManagerTool.DiscordBot.Models
{
    public class DiscordBotConfig
    {
        public bool AllowAllBots { get; set; } = false;

        public List<DiscordBotWhitelist> DiscordBotWhitelists { get; set; } = new List<DiscordBotWhitelist>();
    }
}
