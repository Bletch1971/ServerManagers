using System;
using System.Collections.Generic;

namespace ArkData
{
    public class TribeData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string File { get; set; }
        public string Filename { get; set; }
        public DateTime FileCreated { get; set; }
        public DateTime FileUpdated { get; set; }
        public int? OwnerId { get; set; }
        public virtual ICollection<PlayerData> Players { get; set; }
        public virtual PlayerData Owner { get; set; }

        public TribeData()
        {
            this.Players = new HashSet<PlayerData>();
        }
    }
}
