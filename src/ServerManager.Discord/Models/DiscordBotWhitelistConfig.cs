using System.Collections.Generic;

namespace ServerManagerTool.DiscordBot.Models
{
    public class DiscordBotWhitelistConfig
    {
        public List<DiscordBotWhitelist> DiscordBotWhitelists { get; set; } = new List<DiscordBotWhitelist>();
    }
}
