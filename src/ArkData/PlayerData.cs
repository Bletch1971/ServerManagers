using System;
using System.Collections.Generic;

namespace ArkData
{
    public class PlayerData
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public long CharacterId { get; set; }
        public string CharacterName { get; set; }
        public bool Online { get; set; }
        public string File { get; set; }
        public string Filename { get; set; }
        public DateTime FileCreated { get; set; }
        public DateTime FileUpdated { get; set; }
        public int? TribeId { get; set; }
        public short Level { get; set; }
        public virtual TribeData Tribe { get; set; }
        public virtual List<TribeData> OwnedTribes { get; set; }

        public DateTime LastPlatformUpdateUtc { get; set; }
        public int NoUpdateCount { get; set; }
}
}
