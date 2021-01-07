using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkData.Models
{
    internal class SteamResponse<T>
    {
        public SteamPlayerResponse<T> response { get; set; }
    }
}
