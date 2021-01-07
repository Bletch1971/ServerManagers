/*
 * This file is part of SSQLib.
 *
 *   SSQLib is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   SSQLib is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with SSQLib.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SSQLib
{
    internal class SocketUtils
    {
        private SocketUtils() { }

        internal static byte[] getInfo(IPEndPoint ipe, Packet packet)
        {
            //Create the socket
            Socket srvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Save the max packet size
            int packetSize = 12288;

            //Send/Receive timeouts
            srvSocket.SendTimeout = 3000;
            srvSocket.ReceiveTimeout = 3000;

            try
            {
                //Send the request to the server
                srvSocket.SendTo(packet.outputAsBytes(), ipe);
            }
            catch (SocketException se)
            {
                throw new SSQLServerException("Could not send packet to server {" + se.Message + "}");
            }

            //Create a new receive buffer
            byte[] rcvPacketInfo = new byte[packetSize];
            EndPoint Remote = (EndPoint)ipe;

            try
            {
                //Receive the data from the server
                srvSocket.ReceiveFrom(rcvPacketInfo, ref Remote);
            }
            catch (SocketException se)
            {
                throw new SSQLServerException("Could not receive packet from server {" + se.Message + "}");
            }

            //Send the information back
            return rcvPacketInfo;
        }

        internal static byte[] getInfo(IPEndPoint ipe, byte[] request)
        {
            //Create the socket
            Socket srvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Save the max packet size
            int packetSize = 12288;

            //Send/Receive timeouts
            srvSocket.SendTimeout = 3000;
            srvSocket.ReceiveTimeout = 3000;

            try
            {
                //Send the request to the server
                srvSocket.SendTo(request, ipe);
            }
            catch (SocketException se)
            {
                throw new SSQLServerException("Could not send packet to server {" + se.Message + "}");
            }

            //Create a new receive buffer
            byte[] rcvPacketInfo = new byte[packetSize];
            EndPoint Remote = (EndPoint)ipe;

            try
            {
                //Receive the data from the server
                srvSocket.ReceiveFrom(rcvPacketInfo, ref Remote);
            }
            catch (SocketException se)
            {
                throw new SSQLServerException("Could not receive packet from server {" + se.Message + "}");
            }

            //Send the information back
            return rcvPacketInfo;
        }
    }
}
