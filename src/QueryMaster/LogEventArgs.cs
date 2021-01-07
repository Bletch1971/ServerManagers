using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryMaster
{
    /// <summary>
    /// Serves as base class for all EventArgs.
    /// </summary>
    [Serializable]
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Gets Timestamp.
        /// </summary>
        public DateTime Timestamp { get; internal set; }
    }

    /// <summary>
    /// Provides data for Exception event.
    /// </summary>
    [Serializable]
    public class ExceptionEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets Log line.
        /// </summary>
        public string LogLine { get; internal set; }
    }

    /// <summary>
    /// Provides data for Server cvar event. 
    /// </summary>
    [Serializable]
    public class CvarEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets Cvar name.
        /// </summary>
        public string Cvar { get; internal set; }
        /// <summary>
        /// Gets Cvar Value.
        /// </summary>
        public string Value { get; internal set; }
    }

    /// <summary>
    /// Provides data log start event.
    /// </summary>
    [Serializable]
    public class LogStartEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets Filename.
        /// </summary>
        public string FileName { get; internal set; }
        /// <summary>
        /// Gets Game name.
        /// </summary>
        public string Game { get; internal set; }
        /// <summary>
        /// Gets Protocol version.
        /// </summary>
        public string Protocol { get; internal set; }
        /// <summary>
        /// Gets Release version.
        /// </summary>
        public string Release { get; internal set; }
        /// <summary>
        /// Gets Build version.
        /// </summary>
        public string Build { get; internal set; }
    }

    /// <summary>
    /// Provides data for map loaded event.
    /// </summary>
    [Serializable]
    public class MapLoadEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets Map name.
        /// </summary>
        public string MapName { get; internal set; }
    }

    /// <summary>
    /// Provides data for map started event.
    /// </summary>
    [Serializable]
    public class MapStartEventArgs : MapLoadEventArgs
    {
        /// <summary>
        /// Get map CRC value.
        /// </summary>
        public string MapCRC { get; internal set; }
    }

    /// <summary>
    /// Provides data for rcon event.
    /// </summary>
    [Serializable]
    public class RconEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets Challenge Id of remote client.
        /// </summary>
        public string Challenge { get; internal set; }
        /// <summary>
        /// Gets Password.
        /// </summary>
        public string Password { get; internal set; }
        /// <summary>
        /// Gets command sent by remote client.
        /// </summary>
        public string Command { get; internal set; }
        /// <summary>
        /// Gets IP-Address of client.
        /// </summary>
        public string Ip { get; internal set; }
        /// <summary>
        /// Gets Port number of client
        /// </summary>
        public ushort Port { get; internal set; }
        /// <summary>
        /// Returns true if password sent is valid.
        /// </summary>
        public bool IsValid { get; internal set; }
    }

    /// <summary>
    /// Provides data for servername event.
    /// </summary>
    [Serializable]
    public class ServerNameEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets name of server.
        /// </summary>
        public string Name { get; internal set; }
    }

    /// <summary>
    /// Provides data for server say event.
    /// </summary>
    [Serializable]
    public class ServerSayEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets the message said by server.
        /// </summary>
        public string Message { get; internal set; }
    }

    /// <summary>
    /// Provides data for Playervalidate,playerenteredgame and player disconnected event.
    /// </summary>
    [Serializable]
    public class PlayerEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets Player information.
        /// </summary>
        public PlayerInfo Player { get; internal set; }
    }

    /// <summary>
    /// Provides data for player connect event.
    /// </summary>
    [Serializable]
    public class ConnectEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets IP-Address of client.
        /// </summary>
        public string Ip { get; internal set; }
        /// <summary>
        /// Gets Port number of client.
        /// </summary>
        public ushort Port { get; internal set; }

    }

    /// <summary>
    /// Provides data for playerkicked event.
    /// </summary>
    [Serializable]
    public class KickEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets the name of the admin who kicked the player.
        /// </summary>
        public string Kicker { get; internal set; }
        /// <summary>
        /// Gets the message sent as a reason for the kick.
        /// </summary>
        public string Message { get; internal set; }
    }

    /// <summary>
    /// Provides data for suicide event.
    /// </summary>
    [Serializable]
    public class SuicideEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets the weapon name.
        /// </summary>
        public string Weapon { get; internal set; }
    }

    /// <summary>
    /// Provides data for team selection event.
    /// </summary>
    [Serializable]
    public class TeamSelectionEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets the team name.
        /// </summary>
        public string Team { get; internal set; }
    }

    /// <summary>
    /// Provides data for role selection event.
    /// </summary>
    [Serializable]
    public class RoleSelectionEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets the role name.
        /// </summary>
        public string Role { get; internal set; }
    }

    /// <summary>
    /// Provides data for player name change event.
    /// </summary>
    [Serializable]
    public class NameChangeEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets player's new name.
        /// </summary>
        public string NewName { get; internal set; }
    }

    /// <summary>
    /// Provides data for player killed event.
    /// </summary>
    [Serializable]
    public class KillEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets Victim player's info.
        /// </summary>
        public PlayerInfo Victim { get; internal set; }
        /// <summary>
        /// Gets the name of the weapon used.
        /// </summary>
        public string Weapon { get; internal set; }
    }

    /// <summary>
    /// Provides data for player injured event.
    /// </summary>
    [Serializable]
    public class InjureEventArgs : KillEventArgs
    {
        /// <summary>
        /// Gets damage.
        /// </summary>
        public string Damage { get; internal set; }
    }

    /// <summary>
    /// Provides data for PlayerOnPLayerTriggered event.
    /// </summary>
    [Serializable]
    public class PlayerOnPlayerEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets info about the player who triggered an action.
        /// </summary>
        public PlayerInfo Source { get; internal set; }
        /// <summary>
        /// Gets info about the player on whom the ation was triggered.
        /// </summary>
        public PlayerInfo Target { get; internal set; }
        /// <summary>
        /// Gets the name of the  action performed.
        /// </summary>
        public string Action { get; internal set; }
    }

    /// <summary>
    /// Provides data for Player action event.
    /// </summary>
    [Serializable]
    public class PlayerActionEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets the name of the action performed.
        /// </summary>
        public string Action { get; internal set; }
        /// <summary>
        /// Gets additional data present in the message.
        /// </summary>
        public string ExtraInfo { get; internal set; }
    }

    /// <summary>
    /// Provides data for team action event.
    /// </summary>
    [Serializable]
    public class TeamActionEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets the name of the team who triggered an action.
        /// </summary>
        public string Team { get; internal set; }
        /// <summary>
        /// Gets the name of the action performed.
        /// </summary>
        public string Action { get; internal set; }
    }

    /// <summary>
    /// Provides data for WorldAction event.
    /// </summary>
    [Serializable]
    public class WorldActionEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets the name of the action performed.
        /// </summary>
        public string Action { get; internal set; }
    }

    /// <summary>
    /// Provides data for Say and TeamSay events.
    /// </summary>
    [Serializable]
    public class ChatEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets the message said by player.
        /// </summary>
        public string Message { get; internal set; }
    }

    /// <summary>
    /// Provides data for TeamAlliance event.
    /// </summary>
    [Serializable]
    public class TeamAllianceEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets the name of 1st team.
        /// </summary>
        public string Team1 { get; internal set; }
        /// <summary>
        /// Gets the name of 2nd team.
        /// </summary>
        public string Team2 { get; internal set; }
    }

    /// <summary>
    /// Provides data for TeamScoreReport event.
    /// </summary>
    [Serializable]
    public class TeamScoreReportEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets the name of team.
        /// </summary>
        public string Team { get; internal set; }
        /// <summary>
        /// Gets the score of team.
        /// </summary>
        public string Score { get; internal set; }
        /// <summary>
        /// Gets the player count.
        /// </summary>
        public string PlayerCount { get; internal set; }
        /// <summary>
        /// Gets the additional data present in the message.
        /// </summary>
        public string ExtraInfo { get; internal set; }
    }

    /// <summary>
    /// Provides data for PrivateChat event.
    /// </summary>
    [Serializable]
    public class PrivateChatEventArgs : LogEventArgs
    {
        /// <summary>
        /// Gets Sender Player's info.
        /// </summary>
        public PlayerInfo Sender { get; internal set; }
        /// <summary>
        /// Gets Receiver Player's info.
        /// </summary>
        public PlayerInfo Receiver { get; internal set; }
        /// <summary>
        /// Get the message sent by sender.
        /// </summary>
        public string Message { get; internal set; }
    }

    /// <summary>
    /// Provides data for PlayerScoreReport event.
    /// </summary>
    [Serializable]
    public class PlayerScoreReportEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets player score.
        /// </summary>
        public string Score { get; internal set; }
        /// <summary>
        /// Gets the additional data present in the message.
        /// </summary>
        public string ExtraInfo { get; internal set; }
    }

    /// <summary>
    /// Provides data for WeaponSelect and WeaponAcquired event.
    /// </summary>
    [Serializable]
    public class WeaponEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets name of weapon.
        /// </summary>
        public string Weapon { get; internal set; }
    }
}
