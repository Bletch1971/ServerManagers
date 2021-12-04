using ServerManagerTool.Discord.Delegates;

namespace ServerManagerTool.Discord
{
    public static class DiscordBot
    {
        public const string PREFIX_DELIMITER = "!";

        internal static HandleCommandDelegate HandleCommandCallback
        {
            get;
            set;
        }
    }
}
