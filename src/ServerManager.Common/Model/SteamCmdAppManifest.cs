using System.Collections.Generic;

namespace ServerManagerTool.Common.Model
{
    public class SteamCmdAppManifest
    {
        public string appid { get; set; }

        public List<SteamCmdManifestUserConfig> UserConfig { get; set; }
    }
}
