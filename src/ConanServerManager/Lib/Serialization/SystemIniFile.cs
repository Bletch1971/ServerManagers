using ServerManagerTool.Common.Serialization;
using System;
using System.Collections.Generic;

namespace ServerManagerTool.Lib
{
    public class SystemIniFile : BaseSystemIniFile
    {
        public static readonly Dictionary<Enum, string> IniFileNames = new Dictionary<Enum, string>
        {
            { IniFiles.Engine, Config.Default.ServerEngineConfigFile },
            { IniFiles.Game, Config.Default.ServerGameConfigFile },
            { IniFiles.ServerSettings, Config.Default.ServerSettingsConfigFile },
        };

        public static readonly Dictionary<Enum, string> IniSectionNames = new Dictionary<Enum, string>
        {
            // Engine sections, used by the server manager
            { IniSections.Engine_OnlineSubsystem, "OnlineSubsystem" },
            { IniSections.Engine_OnlineSubsystemSteam, "OnlineSubsystemSteam" },
            { IniSections.Engine_URL, "URL" },

            // Engine sections, not used by server manager

            // Game sections, used by the server manager
            { IniSections.Game_GameSession, "/Script/Engine.GameSession" },
            { IniSections.Game_RconPlugin, "RconPlugin" },

            // Game sections, not used by server manager

            // Server Settings sections, used by the server manager
            { IniSections.ServerSettings_ServerSettings, "ServerSettings" },

            // Server Settings sections, not used by server manager
        };

        public override Dictionary<Enum, string> FileNames => IniFileNames;

        public override Dictionary<Enum, string> SectionNames => IniSectionNames;

        public SystemIniFile(string iniPath)
            : base(iniPath)
        {
        }
    }
}
