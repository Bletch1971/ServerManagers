using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Net;

namespace QueryMaster
{
    static class MasterUtil
    {
        private static readonly byte Header = 0x31;
        internal static byte[] BuildPacket(string endPoint, Region region, IpFilter filter)
        {
            List<byte> msg = new List<byte>();
            msg.Add(Header);
            msg.Add((byte)region);
            msg.AddRange(Util.StringToBytes(endPoint));
            msg.Add(0x00);
            if (filter != null)
                msg.AddRange(Util.StringToBytes(ProcessFilter(filter)));
            msg.Add(0x00);
            return msg.ToArray();
        }
        internal static ReadOnlyCollection<IPEndPoint> ProcessPacket(byte[] packet)
        {
            Parser parser = new Parser(packet);
            List<IPEndPoint> endPoints = new List<IPEndPoint>();
            parser.Skip(6);
            int counter = 6;
            string ip = string.Empty; ;
            int port = 0;
            while (counter != packet.Length)
            {
                ip = parser.ReadByte() + "." + parser.ReadByte() + "." + parser.ReadByte() + "." + parser.ReadByte();
                byte portByte1 = parser.ReadByte();
                byte portByte2 = parser.ReadByte();
                if (BitConverter.IsLittleEndian)
                {
                    port = BitConverter.ToUInt16(new byte[] { portByte2, portByte1 }, 0);
                }
                else
                {
                    port = BitConverter.ToUInt16(new byte[] { portByte1, portByte2 }, 0);
                }
                endPoints.Add(new IPEndPoint(IPAddress.Parse(ip), port));
                counter += 6;
            }
            return endPoints.AsReadOnly();

        }
        internal static string ProcessFilter(IpFilter filter)
        {
            StringBuilder filterStr = new StringBuilder();
            if(filter.IsDedicated)
                filterStr.Append(@"\type\d");
            if (filter.IsSecure)
                filterStr.Append(@"\secure\1");
            if (!string.IsNullOrEmpty(filter.GameDirectory))
                filterStr.Append(@"\gamedir\" + filter.GameDirectory);
            if (!string.IsNullOrEmpty(filter.Map))
                filterStr.Append(@"\map\" + filter.Map);
            if (filter.IsLinux)
                filterStr.Append(@"\linux\1");
            if (filter.IsNotEmpty)
                filterStr.Append(@"\empty\1");
            if (filter.IsNotFull)
                filterStr.Append(@"\full\1");
            if (filter.IsProxy)
                filterStr.Append(@"\proxy\1");
            if (filter.App != 0)
                filterStr.Append(@"\appid\" + filter.App);
            if (filter.NApp != 0)
                filterStr.Append(@"\napp\" + filter.NApp);
            if (filter.IsNoPlayers)
                filterStr.Append(@"\noplayers\1");
            if (filter.IsWhiteListed)
                filterStr.Append(@"\white\1");
            if (!string.IsNullOrEmpty(filter.Sv_Tags))
                filterStr.Append(@"\gametype\" + filter.Sv_Tags);
            if (!string.IsNullOrEmpty(filter.GameData))
                filterStr.Append(@"\gamedata\" + filter.GameData);
            if (!string.IsNullOrEmpty(filter.GameDataOr))
                filterStr.Append(@"\gamedataor\" + filter.GameDataOr);

            if (filter.IpAddr != null && filter.IpAddr.Length > 0)
            {
                foreach (var ipaddr in filter.IpAddr)
                {
                    if (!string.IsNullOrEmpty(ipaddr))
                    {
                        filterStr.Append(@"\gameaddr\").Append(ipaddr);
                    }
                }
            }
            //filterStr.Append('\0');
            return filterStr.ToString();
        }
    }
}
