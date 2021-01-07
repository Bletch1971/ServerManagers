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
    /// Stores information about a player in the server
    /// </summary>
    public class PlayerInfo
    {
        private string name = "";
        private int index = -9999;
        private int kills = -9999;
        private int deaths = -9999;
        private int score = -9999;
        private int ping = -9999;
        private int rate = -9999;
        private float time = 0.0f;

        /// <summary>
        /// Creates a new PlayerInfo object with default values
        /// </summary>
        public PlayerInfo() { }

        /// <summary>
        /// The name of the player
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
        /// The amount of kills the player has (default: -9999)
        /// </summary>
        public int Kills
        {
            get { 
                return this.kills; 
            }

            set { 
                this.kills = value; 
            }
        }

        /// <summary>
        /// The amount of deaths the player has (default: -9999)
        /// </summary>
        public int Deaths
        {
            get
            {
                return this.deaths;
            }

            set
            {
                this.deaths = value;
            }
        }

        /// <summary>
        /// The score of the player (default: -9999)
        /// </summary>
        public int Score
        {
            get
            {
                return this.score;
            }

            set
            {
                this.score = value;
            }
        }

        /// <summary>
        /// The ping of the player (default: -9999)
        /// </summary>
        public int Ping
        {
            get
            {
                return this.ping;
            }

            set
            {
                this.ping = value;
            }
        }

        /// <summary>
        /// The rate(?) of the player (default: -9999)
        /// </summary>
        public int Rate
        {
            get
            {
                return this.rate;
            }

            set
            {
                this.rate = value;
            }
        }

        /// <summary>
        /// The index of the player in the server
        /// </summary>
        public int Index
        {
            get
            {
                return this.index;
            }

            set
            {
                this.index = value;
            }
        }

        /// <summary>
        /// The time the player has been in the server
        /// </summary>
        public float Time
        {
            get
            {
                return this.time;
            }

            set
            {
                this.time = value;
            }
        }
    }
}
