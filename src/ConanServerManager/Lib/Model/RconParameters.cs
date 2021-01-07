using System.Net;
using System.Windows;

namespace ServerManagerTool.Lib
{
    public class RconParameters : PlayerListParameters
    {
        public string RconHost { get; set; }

        public IPAddress RconHostIP
        {
            get
            {
                try
                {
                    var ipAddresses = Dns.GetHostAddresses(RconHost);
                    if (ipAddresses.Length > 0)
                        return ipAddresses[0].MapToIPv4();
                }
                catch {}

                return IPAddress.None;
            }
        }

        public int RconPort { get; set; }

        public string RconPassword { get; set; }

        public double PlayerListWidth { get; set; }
    }
}
