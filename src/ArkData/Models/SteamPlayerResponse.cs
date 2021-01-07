using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkData.Models
{
    internal class SteamPlayerResponse<T>
    {
        public List<T> players { get; set; }
    }
}
