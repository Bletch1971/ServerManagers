using System.Collections.Generic;

namespace ConanData.Models
{
    internal class SteamPlayerResponse<T>
    {
        public List<T> players { get; set; }
    }
}
