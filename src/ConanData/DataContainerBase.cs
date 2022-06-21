using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The container for the Game Server data.
/// </summary>
namespace ConanData
{
    public partial class DataContainer
    {
        /// <summary>
        /// A list of all players registered on the server.
        /// </summary>
        public List<PlayerData> Players { get; set; }
        /// <summary>
        /// A list of all guilds registered on the server.
        /// </summary>
        public List<GuildData> Guilds { get; set; }

        /// <summary>
        /// Constructs the DataContainer.
        /// </summary>
        public DataContainer()
        {
            Players = new List<PlayerData>();
            Guilds = new List<GuildData>();
        }

        /// <summary>
        /// Links the players to their guilds and the guilds to the players.
        /// </summary>
        private void LinkPlayerGuild()
        {
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                player.OwnedGuilds = Guilds.Where(t => t.OwnerId == player.CharacterId).ToList();
                player.Guild = Guilds.SingleOrDefault(t => t.GuildId == player.GuildId);
            }

            for (var i = 0; i < Guilds.Count; i++)
            {
                var guild = Guilds[i];
                guild.Owner = Players.SingleOrDefault(p => p.CharacterId == guild.OwnerId);
                guild.Players = Players.Where(p => p.GuildId == guild.GuildId).ToList();
            }
        }
    }
}
