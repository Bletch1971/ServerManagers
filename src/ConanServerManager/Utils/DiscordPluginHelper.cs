using ServerManagerTool.Lib;
using System.Collections.Generic;
using System.Linq;

namespace ServerManagerTool.Utils
{
    internal static class DiscordPluginHelper
    {
        public static IList<Plugin.Common.Lib.Profile> FetchProfiles()
        {
            return ServerManager.Instance.Servers.Select(s => new ServerManagerTool.Plugin.Common.Lib.Profile()
            {
                ProfileName = s?.Profile?.ProfileName ?? string.Empty,
                InstallationFolder = s?.Profile?.InstallDirectory ?? string.Empty
            }).ToList();
        }
    }
}
