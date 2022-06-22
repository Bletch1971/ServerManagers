using ServerManagerTool.Common.Serialization;
using System;
using System.Collections.Generic;

namespace ServerManagerTool.Lib
{
    public class SystemIniFile : BaseSystemIniFile
    {
        public static readonly Dictionary<Enum, string> IniFileNames = new Dictionary<Enum, string>
        {
            { IniFiles.GameUserSettings, Config.Default.ServerGameUserSettingsConfigFile },
            { IniFiles.Game, Config.Default.ServerGameConfigFile },
            { IniFiles.Engine, Config.Default.ServerEngineConfigFile },
        };

        public static readonly Dictionary<Enum, string> IniSectionNames = new Dictionary<Enum, string>
        {
            // GameUserSettings sections, used by the server manager
            { IniSections.GUS_ServerSettings, "ServerSettings" },
            { IniSections.GUS_ShooterGameUserSettings, "/Script/ShooterGame.ShooterGameUserSettings" },
            { IniSections.GUS_ScalabilityGroups, "ScalabilityGroups" },
            { IniSections.GUS_SessionSettings, "SessionSettings" },
            { IniSections.GUS_GameSession, "/Script/Engine.GameSession"},
            { IniSections.GUS_MultiHome, "MultiHome" },
            { IniSections.GUS_MessageOfTheDay, "MessageOfTheDay" },
            { IniSections.GUS_Ragnarok, "Ragnarok" },

            // GameUserSettings sections, not used by server manager

            // Game sections, used by the server manager
            { IniSections.Game_ShooterGameMode, "/script/shootergame.shootergamemode" },

            // Game sections, not used by server manager
        };

        public override Dictionary<Enum, string> FileNames => IniFileNames;

        public override Dictionary<Enum, string> SectionNames => IniSectionNames;

        public SystemIniFile(string iniPath)
            : base(iniPath)
        {
        }
    }
}
