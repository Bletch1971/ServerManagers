using ServerManagerTool.Discord.Enums;
using System.Collections.Generic;

namespace ServerManagerTool.Utils
{
    internal static class DiscordBotHelper
    {
        public static IList<string> HandleDiscordCommand(CommandType commandType, string serverId, string channelId, string profileId)
        {
            return new List<string>() { $"{commandType}; {serverId}; {channelId}; {profileId ?? "no profile"}" };
        }
    }
}
