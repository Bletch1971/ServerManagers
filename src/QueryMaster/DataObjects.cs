using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryMaster
{
    /// <summary>
    /// Contains information about the server
    /// </summary>

    [Serializable]
    public class ServerInfo
    {
        /// <summary>
        /// Returns true if server replies with Obsolete response format.
        /// </summary>
        public bool IsObsolete { get; internal set; }
        /// <summary>
        /// Socket address of server.
        /// </summary>
        public string Address { get; internal set; }
        /// <summary>
        /// Protocol version used by the server. 
        /// </summary>
        public byte Protocol { get; internal set; }
        /// <summary>
        /// Name of the server. 
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Map the server has currently loaded. 
        /// </summary>
        public string Map { get; internal set; }
        /// <summary>
        ///  	Name of the folder containing the game files. 
        /// </summary>
        public string Directory { get; internal set; }
        /// <summary>
        /// Full name of the game. 
        /// </summary>
        public string Description { get; internal set; }
        /// <summary>
        /// Steam Application ID of game. 
        /// </summary>
        public short Id { get; internal set; }
        /// <summary>
        /// Number of players on the server. 
        /// </summary>
        public int Players { get; internal set; }
        /// <summary>
        /// Maximum number of players the server reports it can hold. 
        /// </summary>
        public byte MaxPlayers { get; internal set; }
        /// <summary>
        /// Number of bots on the server. 
        /// </summary>
        public byte Bots { get; internal set; }
        /// <summary>
        /// Indicates the type of server.(Dedicated/Non-dedicated/Proxy)
        /// </summary>
        public string ServerType { get; internal set; }
        /// <summary>
        /// Indicates the operating system of the server.(Linux/Windows/Mac)
        /// </summary>
        public string Environment { get; internal set; }
        /// <summary>
        /// Indicates whether the server requires a password
        /// </summary>
        public bool IsPrivate { get; internal set; }
        /// <summary>
        /// Specifies whether the server uses VAC.
        /// </summary>
        public bool IsSecure { get; internal set; }
        /// <summary>
        /// Version of the game installed on the server. 
        /// </summary>
        public string GameVersion { get; internal set; }
        /// <summary>
        /// Round-trip delay time.
        /// </summary>
        public long Ping { get; internal set; }
        /// <summary>
        /// Additional information provided by server.
        /// </summary>
        public ExtraInfo Extra { get; internal set; }
        /// <summary>
        /// Valid only if the server is running The Ship. 
        /// </summary>
        public TheShip ShipInfo { get; internal set; }
        /// <summary>
        /// Indicates whether the game is a mod(Halflofe/HalfLifeMod)
        /// </summary>
        /// <remarks>Present only  in Obsolete server responses.</remarks>
        public bool IsModded { get; internal set; }
        /// <summary>
        /// Valid only if IsModded =true
        /// </summary>
        /// <remarks>Present only in Obsolete server responses.</remarks>
        public Mod ModInfo { get; internal set; }

    }

    /// <summary>
    /// Contains extra information about the Ship server
    /// </summary>
    [Serializable]
    public class TheShip
    {
        /// <summary>
        /// Indicates the game mode.(Hunt/Elimination/Duel/Deathmatch/VIP Team/Team Elimination)
        /// </summary>
        public string Mode { get; internal set; }
        /// <summary>
        /// The number of witnesses necessary to have a player arrested. 
        /// </summary>
        public byte Witnesses { get; internal set; }
        /// <summary>
        /// Time (in seconds) before a player is arrested while being witnessed.
        /// </summary>
        public byte Duration { get; internal set; }
    }

    /// <summary>
    /// Contains information about the Mod.
    /// </summary>
    /// <remarks>Present only in Obsolete server responses.</remarks>
    [Serializable]
    public class Mod
    {
        /// <summary>
        /// URL to mod website. 
        /// </summary>
        public string Link { get; internal set; }
        /// <summary>
        /// URL to download the mod. 
        /// </summary>
        public string DownloadLink { get; internal set; }
        /// <summary>
        /// Version of mod installed on server. 
        /// </summary>
        public long Version { get; internal set; }
        /// <summary>
        /// Space (in bytes) the mod takes up. 
        /// </summary>
        public long Size { get; internal set; }
        /// <summary>
        /// Indicates the type of mod.
        /// </summary>
        public bool IsOnlyMultiPlayer { get; internal set; }
        /// <summary>
        /// Indicates whether mod uses its own DLL
        /// </summary>
        public bool IsHalfLifeDll { get; internal set; }
    }

    /// <summary>
    /// Contains information of a player currently in server
    /// </summary>
    [Serializable]
    public class Player
    {
        /// <summary>
        /// Name of the player. 
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Player's score (usually "frags" or "kills".) 
        /// </summary>
        public long Score { get; internal set; }
        /// <summary>
        /// Time  player has been connected to the server.(returns TimeSpan instance)
        /// </summary>
        public TimeSpan Time { get; internal set; }
    }

    /// <summary>
    /// Contains information of a server rule
    /// </summary>
    [Serializable]
    public class Rule
    {
        /// <summary>
        /// Name of the rule. 
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Value of the rule. 
        /// </summary>
        public string Value { get; internal set; }
    }



    /// <summary>
    /// Contains information of a player
    /// </summary>
    [Serializable]
    public class PlayerInfo
    {
        /// <summary>
        /// Name of player
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// UId of player(Steam ID)
        /// </summary>
        public string Uid { get; internal set; }
        /// <summary>
        /// Won Id
        /// </summary>
        public string WonId { get; internal set; }
        /// <summary>
        /// Player's Team Name
        /// </summary>
        public string Team { get; internal set; }
    }

    /// <summary>
    /// Contains extra information about server
    /// </summary>
    [Serializable]
    public class ExtraInfo
    {
        /// <summary>
        /// The server's game port number.
        /// </summary>
        public short Port { get; internal set; }
        /// <summary>
        /// Server's SteamID. 
        /// </summary>
        public int SteamID { get; internal set; }
        /// <summary>
        /// Contains information on Source TV.(if it is Source TV)
        /// </summary>
        public SourceTVInfo SpecInfo { get; internal set; }
        /// <summary>
        /// Tags that describe the game according to the server. 
        /// </summary>
        public string Keywords { get; internal set; }
        /// <summary>
        /// The server's 64-bit GameID.
        /// </summary>
        public int GameId { get; internal set; }
    }
    /// <summary>
    /// Contains information on SourceTV
    /// </summary>
    [Serializable]
    public class SourceTVInfo
    {
        /// <summary>
        /// Spectator port number for SourceTV.
        /// </summary>
        public short Port { get; internal set; }
        /// <summary>
        /// Name of the spectator server for SourceTV.
        /// </summary>
        public string Name { get; internal set; }
    }


}
