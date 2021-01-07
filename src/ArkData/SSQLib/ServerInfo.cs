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

namespace SSQLib
{
    /// <summary>
    /// Stores information about the Source server
    /// </summary>
    public class ServerInfo
    {
        private string name = "";
        private string ip = "";
        private string port = "";
        private string game = "";
        private string gameVersion = "";
        private string appID = "";
        private string map = "";
        private string playerCount = "";
        private string botCount = "";
        private string maxPlayers = "";

        private bool passworded = false;
        private bool vac = false;
        private ServerInfo.DedicatedType dedicated = ServerInfo.DedicatedType.NONE;
        private ServerInfo.OSType os = ServerInfo.OSType.NONE;

        /// <summary>
        /// Creates a new object with default values
        /// </summary>
        public ServerInfo() { }

        /// <summary>
        /// The name of the server
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value;
            }
        }

        /// <summary>
        /// The IP address of the server
        /// </summary>
        public string IP
        {
            get
            {
                return this.ip;
            }

            set
            {
                this.ip = value;
            }
        }

        /// <summary>
        /// The port the server uses
        /// </summary>
        public string Port
        {
            get
            {
                return this.port;
            }

            set
            {
                this.port = value;
            }
        }

        /// <summary>
        /// The game being played on the server (i.e. Team Fortress (tf))
        /// </summary>
        public string Game
        {
            get
            {
                return this.game;
            }

            set
            {
                this.game = value;
            }
        }

        /// <summary>
        /// The game version running on the server
        /// </summary>
        public string Version
        {
            get
            {
                return this.gameVersion;
            }

            set
            {
                this.gameVersion = value;
            }
        }

        /// <summary>
        /// The map currently being played on the server
        /// </summary>
        public string Map
        {
            get
            {
                return this.map;
            }

            set
            {
                this.map = value;
            }
        }

        /// <summary>
        /// The current player count on the server
        /// </summary>
        public string PlayerCount
        {
            get
            {
                return this.playerCount;
            }

            set
            {
                this.playerCount = value;
            }
        }

        /// <summary>
        /// The current bot count on the server
        /// </summary>
        public string BotCount
        {
            get
            {
                return this.botCount;
            }

            set
            {
                this.botCount = value;
            }
        }

        /// <summary>
        /// The max amount of players allowed on the server
        /// </summary>
        public string MaxPlayers
        {
            get
            {
                return this.maxPlayers;
            }

            set
            {
                this.maxPlayers = value;
            }
        }

        /// <summary>
        /// Stores whether the server is passworded or not
        /// </summary>
        public bool Password
        {
            get
            {
                return this.passworded;
            }

            set
            {
                this.passworded = value;
            }
        }

        /// <summary>
        /// Stores whether the server is VAC protected or not
        /// </summary>
        public bool VAC
        {
            get
            {
                return this.vac;
            }

            set
            {
                this.vac = value;
            }
        }

        /// <summary>
        /// Stores the app ID of the game used by the server
        /// </summary>
        public string AppID
        {
            get
            {
                return this.appID;
            }

            set
            {
                this.appID = value;
            }
        }

        /// <summary>
        /// Stores the type of server running (Listen, Dedicated, SourceTV)
        /// </summary>
        public ServerInfo.DedicatedType Dedicated
        {
            get
            {
                return this.dedicated;
            }

            set
            {
                this.dedicated = value;
            }
        }

        /// <summary>
        /// Stores the operating system of the server (Windows, Linux)
        /// </summary>
        public ServerInfo.OSType OS
        {
            get
            {
                return this.os;
            }

            set
            {
                this.os = value;
            }
        }

        /// <summary>
        /// Used to describe the type of server running
        /// </summary>
        public enum DedicatedType
        {
            /// <summary>
            /// Default value
            /// </summary>
            NONE,
            /// <summary>
            /// Listen server (locally hosted)
            /// </summary>
            LISTEN,
            /// <summary>
            /// Dedicated server
            /// </summary>
            DEDICATED,
            /// <summary>
            /// SourceTV server
            /// </summary>
            SOURCETV
        };

        /// <summary>
        /// Used to describe the operating system running on the server
        /// </summary>
        public enum OSType
        {
            /// <summary>
            /// Default value
            /// </summary>
            NONE,
            /// <summary>
            /// Windows server
            /// </summary>
            WINDOWS,
            /// <summary>
            /// Linux server
            /// </summary>
            LINUX
        };
    }
}
