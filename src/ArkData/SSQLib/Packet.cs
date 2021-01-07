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
using System.IO;

namespace SSQLib
{
    internal class Packet
    {
        internal int RequestId = 0;
        internal string Data = "";

        internal Packet() { }

        //Output the packet data as a byte array
        internal byte[] outputAsBytes()
        {
            byte[] data_byte = null;

            if (Data.Length > 0)
            {
                //Create a new packet based on the length of the request
                data_byte = new byte[Data.Length + 5];

                //Fill the first 4 bytes with 0xff
                data_byte[0] = 0xff;
                data_byte[1] = 0xff;
                data_byte[2] = 0xff;
                data_byte[3] = 0xff;

                //Copy the data to the new request
                Array.Copy(ASCIIEncoding.UTF8.GetBytes(Data), 0, data_byte, 4, Data.Length);
            }
            //Empty request to get challenge
            else
            {
                data_byte = new byte[5];

                //Fill the first 4 bytes with 0xff
                data_byte[0] = 0xff;
                data_byte[1] = 0xff;
                data_byte[2] = 0xff;
                data_byte[3] = 0xff;
                data_byte[4] = 0x57;
            }


            return data_byte;
        }
    }
}
