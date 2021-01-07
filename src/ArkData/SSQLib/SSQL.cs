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
using System.Collections;

namespace SSQLib
{
    /// <summary>
    /// Used to retreive information from a Source server
    /// </summary>
    public class SSQL
    {
        /// <summary>
        /// Generates an SSQL object with default values
        /// </summary>
        public SSQL()
        {

        }
        /// <summary>
        /// Pings the specified Source server to retreive information about it such as the server name, max players, current number of players, etc.
        /// </summary>
        /// <param name="ip_end">The IPEndPoint object containing the IP address and port of the server</param>
        /// <returns>Information about the server or throws an SSQLServerException if it could not be retreived</returns>
        public ServerInfo Server(IPEndPoint ip_end)
        {
            //Create a new empty server info object
            ServerInfo info = new ServerInfo();

            //Create an empty buffer
            byte[] buf = null;

            //Create a new packet and request
            Packet requestPacket = new Packet();
            requestPacket.Data = "TSource Engine Query";

            try
            {
                //Attempt to get the server info
                buf = SocketUtils.getInfo(ip_end, requestPacket);
            }
            catch(SSQLServerException e)
            {
                throw e;
            }

            //Start past the first four bytes which are all 0xff
            int i = 4;

            //Make sure the first character is an I
            if (buf[i++] != 'I') return null;

            //Make sure the returned version is above 0x07
            if (buf[i++] < 0x07) return null;

            StringBuilder srvName = new StringBuilder();

            //Retrieve the server name
            while (buf[i] != 0x00)
            {
                srvName.Append((char)buf[i]);
                i++;
            }

            //Move to the next byte
            i++;

            //Set the name of the server
            info.Name = srvName.ToString();

            StringBuilder mapName = new StringBuilder();

            //Retrieve the map name
            while (buf[i] != 0x00)
            {
                mapName.Append((char)buf[i]);
                i++;
            }

            //Move to the next byte
            i++;

            info.Map = mapName.ToString();

            StringBuilder gameName = new StringBuilder();

            //Get the short name for the game
            while (buf[i] != 0x00)
            {
                gameName.Append((char)buf[i]);
                i++;
            }

            //Move to the next byte
            i++;

            StringBuilder gameFriendly = new StringBuilder();

            //Get the friendly game description
            while (buf[i] != 0x00)
            {
                gameFriendly.Append((char)buf[i]);
                i++;
            }

            //Move to the next byte
            i++;

            info.Game = gameFriendly.ToString() + " (" + gameName.ToString() + ")";

            short appID = (short)System.BitConverter.ToInt16(buf, i);

            //Skip the next 2 bytes
            i += 2;

            //Store the app id
            info.AppID = appID.ToString();

            //Get the number of players
            info.PlayerCount = buf[i++].ToString();

            //Get the number of max players
            info.MaxPlayers = buf[i++].ToString();

            //Get the number of bots
            info.BotCount = buf[i++].ToString();

            //Get the dedicated server type
            if ((char)buf[i] == 'l')
                info.Dedicated = ServerInfo.DedicatedType.LISTEN;
            else if ((char)buf[i] == 'd')
                info.Dedicated = ServerInfo.DedicatedType.DEDICATED;
            else if ((char)buf[i] == 'p')
                info.Dedicated = ServerInfo.DedicatedType.SOURCETV;

            //Move to the next byte
            i++;

            //Get the OS type
            if ((char)buf[i] == 'l')
                info.OS = ServerInfo.OSType.LINUX;
            else if ((char)buf[i] == 'w')
                info.OS = ServerInfo.OSType.WINDOWS;

            //Move to the next byte
            i++;

            //Check for password protection
            if (buf[i++] == 0x01) info.Password = true;

            //Check for VAC
            if (buf[i++] == 0x01) info.VAC = true;

            StringBuilder versionInfo = new StringBuilder();

            //Get the game version
            while (buf[i] != 0x00)
            {
                versionInfo.Append((char)buf[i]);
                i++;
            }

            //Move to the next byte
            i++;

            //Set the version
            info.Version = versionInfo.ToString();

            return info;
        }

        /// <summary>
        /// Retreives information about the players on a Source server
        /// </summary>
        /// <param name="ip_end">The IPEndPoint object storing the IP address and port of the server</param>
        /// <returns>An ArrayList of PlayerInfo or throws an SSQLServerException if the server could not be reached</returns>
        public ArrayList Players(IPEndPoint ip_end)
        {
            //Create a new array list to store the player array
            ArrayList players = new ArrayList();

            //Create a new buffer to receive packets
            byte[] buf = null;

            //Create a challenge packet
            byte[] challenge = new byte[9];
            challenge[0] = (byte)0xff;
            challenge[1] = (byte)0xff;
            challenge[2] = (byte)0xff;
            challenge[3] = (byte)0xff;
            challenge[4] = (byte)0x55;
            challenge[5] = (byte)0x00;
            challenge[6] = (byte)0x00;
            challenge[7] = (byte)0x00;
            challenge[8] = (byte)0x00;

            try
            {
                //Attempt to get the challenge response
                buf = SocketUtils.getInfo(ip_end, challenge);
            }
            catch (SSQLServerException e)
            {
                throw e;
            }

            int i = 4;            

            //Make sure the response starts with A
            if (buf[i++] != 'A') return null;

            //Create the new request with the challenge number
            byte[] requestPlayer = new byte[9];

            requestPlayer[0] = (byte)0xff;
            requestPlayer[1] = (byte)0xff;
            requestPlayer[2] = (byte)0xff;
            requestPlayer[3] = (byte)0xff;
            requestPlayer[4] = (byte)0x55;
            requestPlayer[5] = buf[i++];
            requestPlayer[6] = buf[i++];
            requestPlayer[7] = buf[i++];
            requestPlayer[8] = buf[i++];

            try
            {
                //Attempt to get the players response
                buf = SocketUtils.getInfo(ip_end, requestPlayer);
            }
            catch (SSQLServerException)
            {
                return null;
            }

            //Start past 0xffffffff
            i = 4;

            //Make sure the response starts with D
            if (buf[i++] != 'D') return null;

            //Get the amount of players
            byte numPlayers = buf[i++];

            //Loop through each player and extract their stats
            for (int ii = 0; ii < numPlayers; ii++)
            {
                //Create a new player
                PlayerInfo newPlayer = new PlayerInfo();

                //Set the index of the player (Does not work in L4D2, always returns 0)
                newPlayer.Index = buf[i++];

                //Create a new player name
                List<byte> playerName = new List<byte>();

                //Loop through and store the player's name
                while (buf[i] != 0x00)
                {
                    playerName.Add(buf[i++]);
                }

                //Move past the end of the string
                i++;

                // Decode the player name
                newPlayer.Name = Encoding.UTF8.GetString(playerName.ToArray());

                //Get the kills and store them in the player info
                newPlayer.Kills = (int)(buf[i] & 255) | ((buf[i + 1] & 255) << 8) | ((buf[i + 2] & 255) << 16) | ((buf[i + 3] & 255) << 24);

                //Move to the next item
                i += 5;

                //Get the time connected as a float and store it in the player info
                newPlayer.Time = (float)((int)(buf[i] & 255) | ((buf[i + 1] & 255) << 8));

                //Move past the float
                i += 3;

                //Add the player to the list
                players.Add(newPlayer);
            }

            //Return the list of players
            return players;
        }
    }
}
