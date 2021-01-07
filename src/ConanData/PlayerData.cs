using System;
using System.Collections.Generic;

namespace ConanData
{
    public class PlayerData
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public long CharacterId { get; set; }
        public string CharacterName { get; set; }
        public bool Online { get; set; }
        public long? GuildId { get; set; }
        public short Level { get; set; }
        public virtual GuildData Guild { get; set; }
        public virtual List<GuildData> OwnedGuilds { get; set; }
        public int? LastOnline { get; set; }

        public DateTime LastPlatformUpdateUtc { get; set; }
    }
}
