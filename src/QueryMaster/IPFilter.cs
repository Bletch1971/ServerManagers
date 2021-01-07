using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryMaster
{
    /// <summary>
    /// Allows you to restrict the results to servers running a certain game.
    /// </summary>
    public class IpFilter
    {
        /// <summary>
        /// Servers running dedicated 
        /// </summary>
        public bool IsDedicated { get; set; }
        /// <summary>
        /// Servers using anti-cheat technology.(eg:-VAC)
        /// </summary>
        public bool IsSecure { get; set; }
        /// <summary>
        /// Servers running the specified modification(ex. cstrike) 
        /// </summary>
        public string GameDirectory { get; set; }
        /// <summary>
        /// Servers running the specified map 
        /// </summary>
        public string Map { get; set; }
        /// <summary>
        /// Servers running on a Linux platform 
        /// </summary>
        public bool IsLinux { get; set; }
        /// <summary>
        /// Servers that are not empty 
        /// </summary>
        public bool IsNotEmpty { get; set; }
        /// <summary>
        /// Servers that are not full 
        /// </summary>
        public bool IsNotFull { get; set; }
        /// <summary>
        /// Servers that are spectator proxies 
        /// </summary>
        public bool IsProxy { get; set; }
        /// <summary>
        /// Servers running the specified app
        /// </summary>
        public int App { get; set; }
        /// <summary>
        /// Servers that are NOT running a game(AppId)(This was introduced to block Left 4 Dead games from the Steam Server Browser)
        /// </summary>
        public int NApp { get; set; }
        /// <summary>
        /// Servers that are empty 
        /// </summary>
        public bool IsNoPlayers { get; set; }
        /// <summary>
        /// Servers that are whitelisted 
        /// </summary>
        public bool IsWhiteListed { get; set; }
        /// <summary>
        /// Servers with all of the given tag(s) in sv_tags 
        /// </summary>
        public string Sv_Tags { get; set; }
        /// <summary>
        /// Servers with all of the given tag(s) in their 'hidden' tags (L4D2) 
        /// </summary>
        public string GameData { get; set; }
        /// <summary>
        /// Servers with any of the given tag(s) in their 'hidden' tags (L4D2) 
        /// </summary>
        public string GameDataOr { get; set; }

        public string[] IpAddr { get; set; }
    }
}
