using System.Net;
using System.Windows;

namespace ServerManagerTool.Lib
{
    public class RCONParameters : PlayerListParameters
    {
        public static readonly DependencyProperty MaxPlayersProperty = DependencyProperty.Register(nameof(MaxPlayers), typeof(int), typeof(RCONParameters), new PropertyMetadata(0));

        public string RCONHost { get; set; }

        public IPAddress RCONHostIP
        {
            get
            {
                try
                {
                    var ipAddresses = Dns.GetHostAddresses(RCONHost);
                    if (ipAddresses.Length > 0)
                        return ipAddresses[0].MapToIPv4();
                }
                catch {}

                return IPAddress.None;
            }
        }

        public int RCONPort { get; set; }

        public string RCONPassword { get; set; }

        public int MaxPlayers
        {
            get { return (int)GetValue(MaxPlayersProperty); }
            set { SetValue(MaxPlayersProperty, value); }
        }

        public double PlayerListWidth { get; set; }
    }
}
