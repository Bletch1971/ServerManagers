using ServerManagerTool.DiscordBot.Enums;
using System.Collections.Generic;
using System.Threading;

namespace ServerManagerTool.DiscordBot.Delegates
{
    public delegate IList<string> HandleCommandDelegate(CommandType commandType, string serverId, string channelId, string profileIdOrAlias, CancellationToken token);
}
