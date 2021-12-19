using ServerManagerTool.DiscordBot.Enums;

namespace ServerManagerTool.DiscordBot.Models
{
    public class DiscordBotConfig
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        public int MaxArchiveDays { get; set; } = 30;

        public int MaxArchiveFiles { get; set; } = 30;

        public string DiscordToken { get; set; } = string.Empty;

        public string CommandPrefix { get; set; } = string.Empty;

        public string DataDirectory { get; set; } = string.Empty;

        public bool AllowAllBots { get; set; } = false;

        public DiscordBotWhitelist DiscordBotWhitelist { get; set; } = new DiscordBotWhitelist();
    }
}
