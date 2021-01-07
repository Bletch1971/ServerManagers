using System.Collections.Generic;

namespace ConanData
{
    public class GuildData
    {
        public long GuildId { get; set; }
        public string GuildName { get; set; }
        public long? OwnerId { get; set; }
        public virtual ICollection<PlayerData> Players { get; set; }
        public virtual PlayerData Owner { get; set; }

        public GuildData()
        {
            this.Players = new HashSet<PlayerData>();
        }
    }
}
