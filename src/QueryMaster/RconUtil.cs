using System;
using System.Collections.Generic;

namespace QueryMaster
{
    static class RconUtil
    {
        internal static byte[] GetBytes(RconSrcPacket packet)
        {
            byte[] command = Util.StringToBytes(packet.Body);
            packet.Size = 10 + command.Length;
            List<byte> y = new List<byte>(packet.Size + 4);
            y.AddRange(BitConverter.GetBytes(packet.Size));
            y.AddRange(BitConverter.GetBytes(packet.Id));
            y.AddRange(BitConverter.GetBytes(packet.Type));
            y.AddRange(command);
            //part of string
            y.Add(0x00);
            //end terminater
            y.Add(0x00);
            return y.ToArray();
        }

        internal static RconSrcPacket ProcessPacket(byte[] data)
        {
            RconSrcPacket packet = new RconSrcPacket();
            try
            {
                Parser parser = new Parser(data);
                packet.Size = parser.ReadInt();
                packet.Id = parser.ReadInt();
                packet.Type = parser.ReadInt();
                byte[] body = parser.GetUnParsedData();
                if (body.Length == 2)
                    packet.Body = string.Empty;
                else
                {
                    packet.Body = Util.BytesToString(body).TrimEnd('\0', ' ');
                }
            }
            catch (Exception e)
            {
                e.Data.Add("ReceivedData", data);
                throw;
            }
            return packet;
        }
    }
}