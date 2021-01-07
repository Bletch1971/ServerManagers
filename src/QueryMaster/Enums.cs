using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryMaster
{


    /// <summary>
    /// Specifies the type of engine used by server
    /// </summary>
    public enum EngineType
    {
        /// <summary>
        /// Source Engine
        /// </summary>
        Source,
        /// <summary>
        /// Gold Source Engine
        /// </summary>
        GoldSource
    }

    /// <summary>
    /// Specifies the game
    /// </summary>
    public enum Game
    {
        //Gold Source Games
        /// <summary>
        /// Counter-Strike
        /// </summary>
        CounterStrike = 10,
        /// <summary>
        /// Team Fortress Classic
        /// </summary>
        Team_Fortress_Classic = 20,
        /// <summary>
        /// Day Of Defeat
        /// </summary>
        Day_Of_Defeat = 30,
        /// <summary>
        /// Deathmatch Classic
        /// </summary>
        Deathmatch_Classic = 40,
        /// <summary>
        /// Opposing Force
        /// </summary>
        Opposing_Force = 50,
        /// <summary>
        /// Ricochet
        /// </summary>
        Ricochet = 60,
        /// <summary>
        /// Half-Life
        /// </summary>
        Half_Life = 70,
        /// <summary>
        /// Condition Zero
        /// </summary>
        Condition_Zero = 80,
        /// <summary>
        /// CounterStrike 1.6 dedicated server
        /// </summary>
        CounterStrike_1_6_dedicated_server = 90,
        /// <summary>
        /// Condition Zero Deleted Scenes
        /// </summary>
        Condition_Zero_Deleted_Scenes = 100,
        /// <summary>
        /// Half-Life:Blue Shift
        /// </summary>
        Half_Life_Blue_Shift = 130,
        //Source Games
        /// <summary>
        /// Half-Life 2
        /// </summary>
        Half_Life_2 = 220,
        /// <summary>
        /// Counter-Strike: Source
        /// </summary>
        CounterStrike_Source = 240,
        /// <summary>
        /// Half-Life: Source
        /// </summary>
        Half_Life_Source = 280,
        /// <summary>
        /// Day of Defeat: Source
        /// </summary>
        Day_Of_Defeat_Source = 300,
        /// <summary>
        /// Half-Life 2: Deathmatch
        /// </summary>
        Half_Life_2_Deathmatch = 320,
        /// <summary>
        /// Half-Life 2: Lost Coast
        /// </summary>
        Half_Life_2_Lost_Coast = 340,
        /// <summary>
        /// Half-Life Deathmatch: Source
        /// </summary>
        Half_Life_Deathmatch_Source = 360,
        /// <summary>
        /// Half-Life 2: Episode One
        /// </summary>
        Half_Life_2_Episode_One = 380,
        /// <summary>
        /// Portal
        /// </summary>
        Portal = 400,
        /// <summary>
        /// Half-Life 2: Episode Two
        /// </summary>
        Half_Life_2_Episode_Two = 420,
        /// <summary>
        /// Team Fortress 2
        /// </summary>
        Team_Fortress_2 = 440,
        /// <summary>
        /// Left 4 Dead
        /// </summary>
        Left_4_Dead = 500,
        /// <summary>
        /// Left 4 Dead 2
        /// </summary>
        Left_4_Dead_2 = 550,
        /// <summary>
        /// Dota 2 
        /// </summary>
        Dota_2 = 570,
        /// <summary>
        /// Portal 2
        /// </summary>
        Portal_2 = 620,
        /// <summary>
        /// Alien Swarm
        /// </summary>
        Alien_Swarm = 630,
        /// <summary>
        /// Counter-Strike: Global Offensive
        /// </summary>
        CounterStrike_Global_Offensive = 1800,
        /// <summary>
        /// SiN Episodes: Emergence
        /// </summary>
        SiN_Episodes_Emergence = 1300,
        /// <summary>
        /// Dark Messiah of Might and Magic
        /// </summary>
        Dark_Messiah_Of_Might_And_Magic = 2100,
        /// <summary>
        /// Dark Messiah Might and Magic Multi-Player
        /// </summary>
        Dark_Messiah_Might_And_Magic_MultiPlayer = 2130,
        /// <summary>
        /// The Ship
        /// </summary>
        The_Ship = 2400,
        /// <summary>
        /// Bloody Good Time
        /// </summary>
        Bloody_Good_Time = 2450,
        /// <summary>
        /// Vampire The Masquerade - Bloodlines
        /// </summary>
        Vampire_The_Masquerade_Bloodlines = 2600,
        /// <summary>
        /// Garry's Mod
        /// </summary>
        Garrys_Mod = 4000,
        /// <summary>
        /// Zombie Panic! Source
        /// </summary>
        Zombie_Panic_Source = 17500,
        /// <summary>
        /// Age of Chivalry
        /// </summary>
        Age_of_Chivalry = 17510,
        /// <summary>
        /// Synergy
        /// </summary>
        Synergy = 17520,
        /// <summary>
        /// D.I.P.R.I.P.
        /// </summary>
        D_I_P_R_I_P = 17530,
        /// <summary>
        /// Eternal Silence
        /// </summary>
        Eternal_Silence = 17550,
        /// <summary>
        /// Pirates, Vikings, and Knights II
        /// </summary>
        Pirates_Vikings_And_Knights_II = 17570,
        /// <summary>
        /// Dystopia
        /// </summary>
        Dystopia = 17580,
        /// <summary>
        /// Insurgency
        /// </summary>
        Insurgency = 17700,
        /// <summary>
        /// Nuclear Dawn
        /// </summary>
        Nuclear_Dawn = 17710,
        /// <summary>
        /// Smashball
        /// </summary>
        Smashball = 17730,
    }

    /// <summary>
    /// Specifies the Region
    /// </summary>
    public enum Region : byte
    {
        /// <summary>
        /// US East coast 
        /// </summary>
        US_East_coast,
        /// <summary>
        /// 	US West coast 
        /// </summary>
        US_West_coast,
        /// <summary>
        /// South America
        /// </summary>
        South_America,
        /// <summary>
        /// Europe
        /// </summary>
        Europe,
        /// <summary>
        /// Asia
        /// </summary>
        Asia,
        /// <summary>
        /// Australia
        /// </summary>
        Australia,
        /// <summary>
        /// Middle East 
        /// </summary>
        Middle_East,
        /// <summary>
        /// Africa
        /// </summary>
        Africa,
        /// <summary>
        /// Rest of the world 
        /// </summary>
        Rest_of_the_world = 0xFF
    }

    enum SocketType
    {
        Udp,
        Tcp
    }

    enum ResponseMsgHeader : byte
    {
        A2S_INFO = 0x49,
        A2S_INFO_Obsolete = 0x6D,
        A2S_PLAYER = 0x44,
        A2S_RULES = 0x45,
        A2S_SERVERQUERY_GETCHALLENGE = 0x41,
    }

    //Used in Source Rcon
    enum PacketId
    {
        Empty = 10,
        ExecCmd = 11
    }

    enum PacketType
    {
        Auth = 3,
        AuthResponse = 2,
        Exec = 2,
        ExecResponse = 0
    }







}
