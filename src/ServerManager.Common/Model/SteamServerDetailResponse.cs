using System.Collections.Generic;

namespace ServerManagerTool.Common.Model
{
    public class SteamServerDetailResponse
    {
        public string success { get; set; }

        public List<SteamServerDetail> servers { get; set; }
    }
}
