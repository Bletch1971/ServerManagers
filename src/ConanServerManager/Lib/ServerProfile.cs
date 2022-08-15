using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using NeXt.Vdf;
using NLog;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.JsonConverters;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class ServerProfile : DependencyObject
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private string _lastSaveLocation = string.Empty;
        private FileSystemWatcher _serverFilesWatcher = null;

        private ServerProfile()
        {
            ResetProfileId();

            ServerPassword = SecurityUtils.GeneratePassword(16);
            AdminPassword = SecurityUtils.GeneratePassword(16);
            RconPassword = string.Empty;
            BranchPassword = string.Empty;

            ServerFilesBlacklisted = new PlayerUserList();
            ServerFilesWhitelisted = new PlayerUserList();

            // initialise the nullable properties

            // initialise the complex properties

            GetDefaultDirectories();
        }

        #region Properties

        public static bool EnableServerFilesWatcher { get; set; } = true;

        public static bool KeepConfigValues { get; set; } = false;

        #region Profile Properties
        public static readonly DependencyProperty ProfileIDProperty = DependencyProperty.Register(nameof(ProfileID), typeof(string), typeof(ServerProfile), new PropertyMetadata(Guid.NewGuid().ToString()));
        [DataMember]
        public string ProfileID
        {
            get { return (string)GetValue(ProfileIDProperty); }
            set
            {
                SetValue(ProfileIDProperty, value);
                UpdateProfileToolTip();
            }
        }

        public static readonly DependencyProperty ProfileNameProperty = DependencyProperty.Register(nameof(ProfileName), typeof(string), typeof(ServerProfile), new PropertyMetadata(Config.Default.DefaultServerProfileName));
        [DataMember]
        public string ProfileName
        {
            get { return (string)GetValue(ProfileNameProperty); }
            set
            {
                SetValue(ProfileNameProperty, value);
                UpdateProfileToolTip();
            }
        }

        public static readonly DependencyProperty InstallDirectoryProperty = DependencyProperty.Register(nameof(InstallDirectory), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string InstallDirectory
        {
            get { return (string)GetValue(InstallDirectoryProperty); }
            set { SetValue(InstallDirectoryProperty, value); }
        }

        public static readonly DependencyProperty LastInstalledVersionProperty = DependencyProperty.Register(nameof(LastInstalledVersion), typeof(string), typeof(ServerProfile), new PropertyMetadata(new Version(0, 0).ToString()));
        [DataMember]
        public string LastInstalledVersion
        {
            get { return (string)GetValue(LastInstalledVersionProperty); }
            set { SetValue(LastInstalledVersionProperty, value); }
        }

        public static readonly DependencyProperty SequenceProperty = DependencyProperty.Register(nameof(Sequence), typeof(int), typeof(ServerProfile), new PropertyMetadata(99));
        [DataMember]
        public int Sequence
        {
            get { return (int)GetValue(SequenceProperty); }
            set { SetValue(SequenceProperty, value); }
        }

        public static readonly DependencyProperty LastStartedProperty = DependencyProperty.Register(nameof(LastStarted), typeof(DateTime), typeof(ServerProfile), new PropertyMetadata(DateTime.MinValue));
        [DataMember]
        public DateTime LastStarted
        {
            get { return (DateTime)GetValue(LastStartedProperty); }
            set { SetValue(LastStartedProperty, value); }
        }

        public static readonly DependencyProperty ProfileToolTipProperty = DependencyProperty.Register(nameof(ProfileToolTip), typeof(string), typeof(ServerProfile), new PropertyMetadata(string.Empty));
        public string ProfileToolTip
        {
            get { return (string)GetValue(ProfileToolTipProperty); }
            set { SetValue(ProfileToolTipProperty, value); }
        }

        public string SortKey
        {
            get
            {
                return $"{Sequence:0000000000}|{ProfileName}|{ProfileID}";
            }
        }
        #endregion

        #region Administration
        public static readonly DependencyProperty ServerNameProperty = DependencyProperty.Register(nameof(ServerName), typeof(string), typeof(ServerProfile), new PropertyMetadata(Config.Default.DefaultServerName));
        [DataMember]
        [IniFileEntry(IniFiles.Engine, IniSections.Engine_OnlineSubsystem, ServerProfileCategory.Administration)]
        public string ServerName
        {
            get { return (string)GetValue(ServerNameProperty); }
            set
            {
                SetValue(ServerNameProperty, value);
                ValidateServerName();
            }
        }

        public static readonly DependencyProperty ServerNameLengthProperty = DependencyProperty.Register(nameof(ServerNameLength), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        public int ServerNameLength
        {
            get { return (int)GetValue(ServerNameLengthProperty); }
            set { SetValue(ServerNameLengthProperty, value); }
        }

        public static readonly DependencyProperty ServerNameLengthToLongProperty = DependencyProperty.Register(nameof(ServerNameLengthToLong), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool ServerNameLengthToLong
        {
            get { return (bool)GetValue(ServerNameLengthToLongProperty); }
            set { SetValue(ServerNameLengthToLongProperty, value); }
        }

        public static readonly DependencyProperty ServerPasswordProperty = DependencyProperty.Register(nameof(ServerPassword), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        [IniFileEntry(IniFiles.Engine, IniSections.Engine_OnlineSubsystem, ServerProfileCategory.Administration)]
        public string ServerPassword
        {
            get { return (string)GetValue(ServerPasswordProperty); }
            set { SetValue(ServerPasswordProperty, value); }
        }

        public static readonly DependencyProperty AdminPasswordProperty = DependencyProperty.Register(nameof(AdminPassword), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration)]
        public string AdminPassword
        {
            get { return (string)GetValue(AdminPasswordProperty); }
            set { SetValue(AdminPasswordProperty, value); }
        }

        public static readonly DependencyProperty ServerIPProperty = DependencyProperty.Register(nameof(ServerIP), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string ServerIP
        {
            get { return (string)GetValue(ServerIPProperty); }
            set { SetValue(ServerIPProperty, value); }
        }

        public static readonly DependencyProperty ServerPortProperty = DependencyProperty.Register(nameof(ServerPort), typeof(int), typeof(ServerProfile), new PropertyMetadata(7777));
        [DataMember]
        [IniFileEntry(IniFiles.Engine, IniSections.Engine_URL, ServerProfileCategory.Administration, "Port")]
        public int ServerPort
        {
            get { return (int)GetValue(ServerPortProperty); }
            set 
            { 
                SetValue(ServerPortProperty, value);
                ServerPeerPort = value + 1;
                UpdatePortsString();
            }
        }

        public static readonly DependencyProperty ServerPeerPortProperty = DependencyProperty.Register(nameof(ServerPeerPort), typeof(int), typeof(ServerProfile), new PropertyMetadata(7778));
        [IniFileEntry(IniFiles.Engine, IniSections.Engine_URL, ServerProfileCategory.Administration, "PeerPort", ClearWhenOff = nameof(KeepConfigValues))]
        public int ServerPeerPort
        {
            get { return (int)GetValue(ServerPeerPortProperty); }
            set 
            { 
                SetValue(ServerPeerPortProperty, value);
                UpdatePortsString();
            }
        }

        public static readonly DependencyProperty QueryPortProperty = DependencyProperty.Register(nameof(QueryPort), typeof(int), typeof(ServerProfile), new PropertyMetadata(27015));
        [DataMember]
        [IniFileEntry(IniFiles.Engine, IniSections.Engine_OnlineSubsystemSteam, ServerProfileCategory.Administration, "GameServerQueryPort", ClearWhenOff = nameof(KeepConfigValues))]
        public int QueryPort
        {
            get { return (int)GetValue(QueryPortProperty); }
            set 
            { 
                SetValue(QueryPortProperty, value);
                UpdatePortsString();
            }
        }

        public static readonly DependencyProperty PortsStringProperty = DependencyProperty.Register(nameof(PortsString), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        public string PortsString
        {
            get { return (string)GetValue(PortsStringProperty); }
            set { SetValue(PortsStringProperty, value); }
        }

        public static readonly DependencyProperty ServerMapProperty = DependencyProperty.Register(nameof(ServerMap), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string ServerMap
        {
            get { return (string)GetValue(ServerMapProperty); }
            set 
            { 
                SetValue(ServerMapProperty, value);
                SyncMapSaveFileName();
            }
        }

        public static readonly DependencyProperty ServerMapSaveFileNameProperty = DependencyProperty.Register(nameof(ServerMapSaveFileName), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string ServerMapSaveFileName
        {
            get { return (string)GetValue(ServerMapSaveFileNameProperty); }
            set { SetValue(ServerMapSaveFileNameProperty, value); }
        }

        public static readonly DependencyProperty ServerModIdsProperty = DependencyProperty.Register(nameof(ServerModIds), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string ServerModIds
        {
            get { return (string)GetValue(ServerModIdsProperty); }
            set { SetValue(ServerModIdsProperty, value); }
        }

        public static readonly DependencyProperty RconEnabledProperty = DependencyProperty.Register(nameof(RconEnabled), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        [IniFileEntry(IniFiles.Game, IniSections.Game_RconPlugin, ServerProfileCategory.Administration, ConditionedOn = nameof(RconEnabled), WriteBooleanAsInteger = true)]
        public bool RconEnabled
        {
            get { return (bool)GetValue(RconEnabledProperty); }
            set { SetValue(RconEnabledProperty, value); }
        }

        public static readonly DependencyProperty RconPortProperty = DependencyProperty.Register(nameof(RconPort), typeof(int), typeof(ServerProfile), new PropertyMetadata(25575));
        [DataMember]
        [IniFileEntry(IniFiles.Game, IniSections.Game_RconPlugin, ServerProfileCategory.Administration, ConditionedOn = nameof(RconEnabled))]
        public int RconPort
        {
            get { return (int)GetValue(RconPortProperty); }
            set { SetValue(RconPortProperty, value); }
        }

        public static readonly DependencyProperty RconPasswordProperty = DependencyProperty.Register(nameof(RconPassword), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        [IniFileEntry(IniFiles.Game, IniSections.Game_RconPlugin, ServerProfileCategory.Administration, ConditionedOn = nameof(RconEnabled))]
        public string RconPassword
        {
            get { return (string)GetValue(RconPasswordProperty); }
            set { SetValue(RconPasswordProperty, value); }
        }

        public static readonly DependencyProperty MOTDProperty = DependencyProperty.Register(nameof(MOTD), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration, "ServerMessageOfTheDay", Multiline = true, MultilineSeparator = "<br>", QuotedString = QuotedStringType.Remove)]
        public string MOTD
        {
            get { return (string)GetValue(MOTDProperty); }
            set
            {
                SetValue(MOTDProperty, value);
                ValidateMOTD();
            }
        }

        public static readonly DependencyProperty MOTDLineCountProperty = DependencyProperty.Register(nameof(MOTDLineCount), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        public int MOTDLineCount
        {
            get { return (int)GetValue(MOTDLineCountProperty); }
            set { SetValue(MOTDLineCountProperty, value); }
        }

        public static readonly DependencyProperty MOTDLineCountToLongProperty = DependencyProperty.Register(nameof(MOTDLineCountToLong), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool MOTDLineCountToLong
        {
            get { return (bool)GetValue(MOTDLineCountToLongProperty); }
            set { SetValue(MOTDLineCountToLongProperty, value); }
        }

        public static readonly DependencyProperty MOTDLengthProperty = DependencyProperty.Register(nameof(MOTDLength), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        public int MOTDLength
        {
            get { return (int)GetValue(MOTDLengthProperty); }
            set { SetValue(MOTDLengthProperty, value); }
        }

        public static readonly DependencyProperty MOTDLengthToLongProperty = DependencyProperty.Register(nameof(MOTDLengthToLong), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool MOTDLengthToLong
        {
            get { return (bool)GetValue(MOTDLengthToLongProperty); }
            set { SetValue(MOTDLengthToLongProperty, value); }
        }

        public static readonly DependencyProperty MOTDIntervalEnabledProperty = DependencyProperty.Register(nameof(MOTDIntervalEnabled), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool MOTDIntervalEnabled
        {
            get { return (bool)GetValue(MOTDIntervalEnabledProperty); }
            set { SetValue(MOTDIntervalEnabledProperty, value); }
        }

        public static readonly DependencyProperty MOTDIntervalProperty = DependencyProperty.Register(nameof(MOTDInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(60));
        [DataMember]
        public int MOTDInterval
        {
            get { return (int)GetValue(MOTDIntervalProperty); }
            set { SetValue(MOTDIntervalProperty, value); }
        }

        public static readonly DependencyProperty UseVACProperty = DependencyProperty.Register(nameof(UseVAC), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration, "IsVACEnabled")]
        public bool UseVAC
        {
            get { return (bool)GetValue(UseVACProperty); }
            set { SetValue(UseVACProperty, value); }
        }

        public static readonly DependencyProperty UseBattlEyeProperty = DependencyProperty.Register(nameof(UseBattlEye), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration, "IsBattlEyeEnabled")]
        public bool UseBattlEye
        {
            get { return (bool)GetValue(UseBattlEyeProperty); }
            set { SetValue(UseBattlEyeProperty, value); }
        }

        public static readonly DependencyProperty AllowFamilySharedAccountProperty = DependencyProperty.Register(nameof(AllowFamilySharedAccount), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration)]
        public bool AllowFamilySharedAccount
        {
            get { return (bool)GetValue(AllowFamilySharedAccountProperty); }
            set { SetValue(AllowFamilySharedAccountProperty, value); }
        }

        public static readonly DependencyProperty ServerRegionProperty = DependencyProperty.Register(nameof(ServerRegion), typeof(string), typeof(ServerProfile), new PropertyMetadata("0"));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration, QuotedString = QuotedStringType.Remove)]
        public string ServerRegion
        {
            get { return (string)GetValue(ServerRegionProperty); }
            set { SetValue(ServerRegionProperty, value); }
        }

        public static readonly DependencyProperty MaxPlayersProperty = DependencyProperty.Register(nameof(MaxPlayers), typeof(int), typeof(ServerProfile), new PropertyMetadata(70));
        [IniFileEntry(IniFiles.Game, IniSections.Game_GameSession, ServerProfileCategory.Administration)]
        public int MaxPlayers
        {
            get { return (int)GetValue(MaxPlayersProperty); }
            set { SetValue(MaxPlayersProperty, value); }
        }

        public static readonly DependencyProperty KickIdlePlayersPercentageProperty = DependencyProperty.Register(nameof(KickIdlePlayersPercentage), typeof(int), typeof(ServerProfile), new PropertyMetadata(80));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration, "KickAFKPercentage")]
        public int KickIdlePlayersPercentage
        {
            get { return (int)GetValue(KickIdlePlayersPercentageProperty); }
            set { SetValue(KickIdlePlayersPercentageProperty, value); }
        }

        public static readonly DependencyProperty KickIdlePlayersPeriodProperty = DependencyProperty.Register(nameof(KickIdlePlayersPeriod), typeof(int), typeof(ServerProfile), new PropertyMetadata(3600));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration, "KickAFKTime")]
        public int KickIdlePlayersPeriod
        {
            get { return (int)GetValue(KickIdlePlayersPeriodProperty); }
            set { SetValue(KickIdlePlayersPeriodProperty, value); }
        }

        public static readonly DependencyProperty ServerTransferEnabledProperty = DependencyProperty.Register(nameof(ServerTransferEnabled), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration)]
        public bool ServerTransferEnabled
        {
            get { return (bool)GetValue(ServerTransferEnabledProperty); }
            set { SetValue(ServerTransferEnabledProperty, value); }
        }

        public static readonly DependencyProperty CanImportDirectlyFromSameServerProperty = DependencyProperty.Register(nameof(CanImportDirectlyFromSameServer), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.Administration)]
        public bool CanImportDirectlyFromSameServer
        {
            get { return (bool)GetValue(CanImportDirectlyFromSameServerProperty); }
            set { SetValue(CanImportDirectlyFromSameServerProperty, value); }
        }

        public static readonly DependencyProperty OutputServerLogProperty = DependencyProperty.Register(nameof(OutputServerLog), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool OutputServerLog
        {
            get { return (bool)GetValue(OutputServerLogProperty); }
            set { SetValue(OutputServerLogProperty, value); }
        }

        public static readonly DependencyProperty ProcessPriorityProperty = DependencyProperty.Register(nameof(ProcessPriority), typeof(string), typeof(ServerProfile), new PropertyMetadata("normal"));
        [DataMember]
        public string ProcessPriority
        {
            get { return (string)GetValue(ProcessPriorityProperty); }
            set { SetValue(ProcessPriorityProperty, value); }
        }

        public static readonly DependencyProperty ProcessAffinityProperty = DependencyProperty.Register(nameof(ProcessAffinity), typeof(BigInteger), typeof(ServerProfile), new PropertyMetadata(BigInteger.Zero));
        [DataMember]
        public BigInteger ProcessAffinity
        {
            get { return (BigInteger)GetValue(ProcessAffinityProperty); }
            set { SetValue(ProcessAffinityProperty, value); }
        }

        public static readonly DependencyProperty AdditionalArgsProperty = DependencyProperty.Register(nameof(AdditionalArgs), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string AdditionalArgs
        {
            get { return (string)GetValue(AdditionalArgsProperty); }
            set { SetValue(AdditionalArgsProperty, value); }
        }

        public static readonly DependencyProperty LauncherArgsProperty = DependencyProperty.Register(nameof(LauncherArgs), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string LauncherArgs
        {
            get { return (string)GetValue(LauncherArgsProperty); }
            set { SetValue(LauncherArgsProperty, value); }
        }

        public static readonly DependencyProperty LauncherArgsOverrideProperty = DependencyProperty.Register(nameof(LauncherArgsOverride), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool LauncherArgsOverride
        {
            get { return (bool)GetValue(LauncherArgsOverrideProperty); }
            set { SetValue(LauncherArgsOverrideProperty, value); }
        }

        public static readonly DependencyProperty LauncherArgsPrefixProperty = DependencyProperty.Register(nameof(LauncherArgsPrefix), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool LauncherArgsPrefix
        {
            get { return (bool)GetValue(LauncherArgsPrefixProperty); }
            set { SetValue(LauncherArgsPrefixProperty, value); }
        }
        #endregion

        #region Automatic Management
        public static readonly DependencyProperty EnableAutoBackupProperty = DependencyProperty.Register(nameof(EnableAutoBackup), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableAutoBackup
        {
            get { return (bool)GetValue(EnableAutoBackupProperty); }
            set { SetValue(EnableAutoBackupProperty, value); }
        }

        public static readonly DependencyProperty EnableAutoStartProperty = DependencyProperty.Register(nameof(EnableAutoStart), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableAutoStart
        {
            get { return (bool)GetValue(EnableAutoStartProperty); }
            set { SetValue(EnableAutoStartProperty, value); }
        }

        public static readonly DependencyProperty AutoStartOnLoginProperty = DependencyProperty.Register(nameof(AutoStartOnLogin), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool AutoStartOnLogin
        {
            get { return (bool)GetValue(AutoStartOnLoginProperty); }
            set { SetValue(AutoStartOnLoginProperty, value); }
        }

        public static readonly DependencyProperty EnableAutoUpdateProperty = DependencyProperty.Register(nameof(EnableAutoUpdate), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableAutoUpdate
        {
            get { return (bool)GetValue(EnableAutoUpdateProperty); }
            set { SetValue(EnableAutoUpdateProperty, value); }
        }

        public static readonly DependencyProperty EnableAutoShutdown1Property = DependencyProperty.Register(nameof(EnableAutoShutdown1), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableAutoShutdown1
        {
            get { return (bool)GetValue(EnableAutoShutdown1Property); }
            set { SetValue(EnableAutoShutdown1Property, value); }
        }

        public static readonly DependencyProperty AutoShutdownTime1Property = DependencyProperty.Register(nameof(AutoShutdownTime1), typeof(string), typeof(ServerProfile), new PropertyMetadata("00:00"));
        [DataMember]
        public string AutoShutdownTime1
        {
            get { return (string)GetValue(AutoShutdownTime1Property); }
            set { SetValue(AutoShutdownTime1Property, value); }
        }

        public static readonly DependencyProperty ShutdownDaysOfTheWeek1Property = DependencyProperty.Register(nameof(ShutdownDaysOfTheWeek1), typeof(DaysOfTheWeek), typeof(ServerProfile), new PropertyMetadata(DaysOfTheWeek.AllDays));
        [DataMember]
        public DaysOfTheWeek ShutdownDaysOfTheWeek1
        {
            get { return (DaysOfTheWeek)GetValue(ShutdownDaysOfTheWeek1Property); }
            set { SetValue(ShutdownDaysOfTheWeek1Property, value); }
        }

        public static readonly DependencyProperty RestartAfterShutdown1Property = DependencyProperty.Register(nameof(RestartAfterShutdown1), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool RestartAfterShutdown1
        {
            get { return (bool)GetValue(RestartAfterShutdown1Property); }
            set { SetValue(RestartAfterShutdown1Property, value); }
        }

        public static readonly DependencyProperty UpdateAfterShutdown1Property = DependencyProperty.Register(nameof(UpdateAfterShutdown1), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UpdateAfterShutdown1
        {
            get { return (bool)GetValue(UpdateAfterShutdown1Property); }
            set { SetValue(UpdateAfterShutdown1Property, value); }
        }

        public static readonly DependencyProperty EnableAutoShutdown2Property = DependencyProperty.Register(nameof(EnableAutoShutdown2), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableAutoShutdown2
        {
            get { return (bool)GetValue(EnableAutoShutdown2Property); }
            set { SetValue(EnableAutoShutdown2Property, value); }
        }

        public static readonly DependencyProperty AutoShutdownTime2Property = DependencyProperty.Register(nameof(AutoShutdownTime2), typeof(string), typeof(ServerProfile), new PropertyMetadata("00:00"));
        [DataMember]
        public string AutoShutdownTime2
        {
            get { return (string)GetValue(AutoShutdownTime2Property); }
            set { SetValue(AutoShutdownTime2Property, value); }
        }

        public static readonly DependencyProperty ShutdownDaysOfTheWeek2Property = DependencyProperty.Register(nameof(ShutdownDaysOfTheWeek2), typeof(DaysOfTheWeek), typeof(ServerProfile), new PropertyMetadata(DaysOfTheWeek.AllDays));
        [DataMember]
        public DaysOfTheWeek ShutdownDaysOfTheWeek2
        {
            get { return (DaysOfTheWeek)GetValue(ShutdownDaysOfTheWeek2Property); }
            set { SetValue(ShutdownDaysOfTheWeek2Property, value); }
        }

        public static readonly DependencyProperty RestartAfterShutdown2Property = DependencyProperty.Register(nameof(RestartAfterShutdown2), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool RestartAfterShutdown2
        {
            get { return (bool)GetValue(RestartAfterShutdown2Property); }
            set { SetValue(RestartAfterShutdown2Property, value); }
        }

        public static readonly DependencyProperty UpdateAfterShutdown2Property = DependencyProperty.Register(nameof(UpdateAfterShutdown2), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UpdateAfterShutdown2
        {
            get { return (bool)GetValue(UpdateAfterShutdown2Property); }
            set { SetValue(UpdateAfterShutdown2Property, value); }
        }

        public static readonly DependencyProperty AutoRestartIfShutdownProperty = DependencyProperty.Register(nameof(AutoRestartIfShutdown), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool AutoRestartIfShutdown
        {
            get { return (bool)GetValue(AutoRestartIfShutdownProperty); }
            set { SetValue(AutoRestartIfShutdownProperty, value); }
        }
        #endregion

        #region Discord Bot
        public static readonly DependencyProperty DiscordChannelIdProperty = DependencyProperty.Register(nameof(DiscordChannelId), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string DiscordChannelId
        {
            get { return (string)GetValue(DiscordChannelIdProperty); }
            set { SetValue(DiscordChannelIdProperty, value); }
        }

        public static readonly DependencyProperty DiscordAliasProperty = DependencyProperty.Register(nameof(DiscordAlias), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string DiscordAlias
        {
            get { return (string)GetValue(DiscordAliasProperty); }
            set { SetValue(DiscordAliasProperty, value); }
        }

        public static readonly DependencyProperty AllowDiscordBackupProperty = DependencyProperty.Register(nameof(AllowDiscordBackup), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool AllowDiscordBackup
        {
            get { return (bool)GetValue(AllowDiscordBackupProperty); }
            set { SetValue(AllowDiscordBackupProperty, value); }
        }

        public static readonly DependencyProperty AllowDiscordRestartProperty = DependencyProperty.Register(nameof(AllowDiscordRestart), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool AllowDiscordRestart
        {
            get { return (bool)GetValue(AllowDiscordRestartProperty); }
            set { SetValue(AllowDiscordRestartProperty, value); }
        }

        public static readonly DependencyProperty AllowDiscordShutdownProperty = DependencyProperty.Register(nameof(AllowDiscordShutdown), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool AllowDiscordShutdown
        {
            get { return (bool)GetValue(AllowDiscordShutdownProperty); }
            set { SetValue(AllowDiscordShutdownProperty, value); }
        }

        public static readonly DependencyProperty AllowDiscordStartProperty = DependencyProperty.Register(nameof(AllowDiscordStart), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool AllowDiscordStart
        {
            get { return (bool)GetValue(AllowDiscordStartProperty); }
            set { SetValue(AllowDiscordStartProperty, value); }
        }

        public static readonly DependencyProperty AllowDiscordStopProperty = DependencyProperty.Register(nameof(AllowDiscordStop), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool AllowDiscordStop
        {
            get { return (bool)GetValue(AllowDiscordStopProperty); }
            set { SetValue(AllowDiscordStopProperty, value); }
        }

        public static readonly DependencyProperty AllowDiscordUpdateProperty = DependencyProperty.Register(nameof(AllowDiscordUpdate), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool AllowDiscordUpdate
        {
            get { return (bool)GetValue(AllowDiscordUpdateProperty); }
            set { SetValue(AllowDiscordUpdateProperty, value); }
        }
        #endregion

        #region Server Details
        public static readonly DependencyProperty UseTestliveProperty = DependencyProperty.Register(nameof(UseTestlive), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UseTestlive
        {
            get { return (bool)GetValue(UseTestliveProperty); }
            set { SetValue(UseTestliveProperty, value); }
        }

        public static readonly DependencyProperty BranchNameProperty = DependencyProperty.Register(nameof(BranchName), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string BranchName
        {
            get { return (string)GetValue(BranchNameProperty); }
            set { SetValue(BranchNameProperty, value); }
        }

        public static readonly DependencyProperty BranchPasswordProperty = DependencyProperty.Register(nameof(BranchPassword), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string BranchPassword
        {
            get { return (string)GetValue(BranchPasswordProperty); }
            set { SetValue(BranchPasswordProperty, value); }
        }
        #endregion

        #region Server Files 
        public static readonly DependencyProperty ServerFilesBlacklistedProperty = DependencyProperty.Register(nameof(ServerFilesBlacklisted), typeof(PlayerUserList), typeof(ServerProfile), new PropertyMetadata(null));
        [DataMember]
        public PlayerUserList ServerFilesBlacklisted
        {
            get { return (PlayerUserList)GetValue(ServerFilesBlacklistedProperty); }
            set { SetValue(ServerFilesBlacklistedProperty, value); }
        }

        public static readonly DependencyProperty EnableWhitelistProperty = DependencyProperty.Register(nameof(EnableWhitelist), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.ServerSettings, IniSections.ServerSettings_ServerSettings, ServerProfileCategory.ServerFiles)]
        public bool EnableWhitelist
        {
            get { return (bool)GetValue(EnableWhitelistProperty); }
            set { SetValue(EnableWhitelistProperty, value); }
        }

        public static readonly DependencyProperty ServerFilesWhitelistedProperty = DependencyProperty.Register(nameof(ServerFilesWhitelisted), typeof(PlayerUserList), typeof(ServerProfile), new PropertyMetadata(null));
        [DataMember]
        public PlayerUserList ServerFilesWhitelisted
        {
            get { return (PlayerUserList)GetValue(ServerFilesWhitelistedProperty); }
            set { SetValue(ServerFilesWhitelistedProperty, value); }
        }
        #endregion

        #region RCON
        public static readonly DependencyProperty RconWindowExtentsProperty = DependencyProperty.Register(nameof(RconWindowExtents), typeof(Rect), typeof(ServerProfile), new PropertyMetadata(new Rect(0f, 0f, 0f, 0f)));
        [DataMember]
        public Rect RconWindowExtents
        {
            get { return (Rect)GetValue(RconWindowExtentsProperty); }
            set { SetValue(RconWindowExtentsProperty, value); }
        }

        public static readonly DependencyProperty RconPlayerListWidthProperty = DependencyProperty.Register(nameof(RconPlayerListWidth), typeof(double), typeof(ServerProfile), new PropertyMetadata(200d));
        [DataMember]
        public double RconPlayerListWidth
        {
            get { return (double)GetValue(RconPlayerListWidthProperty); }
            set { SetValue(RconPlayerListWidthProperty, value); }
        }
        #endregion

        #region Player List
        public static readonly DependencyProperty PlayerListWindowExtentsProperty = DependencyProperty.Register(nameof(PlayerListWindowExtents), typeof(Rect), typeof(ServerProfile), new PropertyMetadata(new Rect(0f, 0f, 0f, 0f)));
        [DataMember]
        public Rect PlayerListWindowExtents
        {
            get { return (Rect)GetValue(PlayerListWindowExtentsProperty); }
            set { SetValue(PlayerListWindowExtentsProperty, value); }
        }
        #endregion

        #endregion

        #region Methods

        #region Common Methods
        public void ChangeInstallationFolder(string folder, bool reloadConfigFiles)
        {
            InstallDirectory = folder;

            if (reloadConfigFiles)
            {
                var serverConfigPath = GetProfileServerConfigDir();
                if (Directory.Exists(serverConfigPath))
                {
                    var serverConfigFile = Path.Combine(serverConfigPath, Config.Default.ServerGameConfigFile);
                    if (File.Exists(serverConfigFile))
                    {
                        LoadFromConfigFiles(serverConfigFile, this, exclusions: null);
                    }
                }
            }

            LoadServerFiles(true, true);
            SetupServerFilesWatcher();
        }

        private void CheckLauncherArgs()
        {
            // do not process if overriding the launcher args
            if (this.LauncherArgsOverride || this.LauncherArgsPrefix)
                return;

            var launcherArgs = LauncherArgs?.ToLower() ?? string.Empty;

            // check Launcher Args for priority
            var priorityValues = ProcessUtils.GetProcessPriorityList();
            foreach (var priority in priorityValues)
            {
                var priorityArg = $"/{priority}";
                if (launcherArgs.Contains(priorityArg))
                {
                    launcherArgs = launcherArgs.Replace(priorityArg, "");
                    ProcessPriority = priority;
                }
            }

            LauncherArgs = launcherArgs;

            if (string.IsNullOrWhiteSpace(LauncherArgs))
            {
                LauncherArgs = string.Empty;

                // check if the launcher args override is enabled, but nothing has been defined in the launcher args
                if (LauncherArgsOverride)
                    LauncherArgsOverride = false;
            }
        }

        public void ClearSteamAppManifestBranch()
        {
            if (!string.IsNullOrWhiteSpace(BranchName))
                return;

            try
            {
                var appIdServer = UseTestlive ? Config.Default.AppIdServer_Testlive : Config.Default.AppIdServer;
                var manifestFile = ModUtils.GetSteamManifestFile(InstallDirectory, appIdServer);
                if (string.IsNullOrWhiteSpace(manifestFile) || !File.Exists(manifestFile))
                    return;

                // load the manifest files
                var vdfDeserializer = VdfDeserializer.FromFile(manifestFile);
                var vdf = vdfDeserializer.Deserialize();

                // clear any of th beta keys values
                var updated = SteamCmdManifestDetailsResult.ClearUserConfigBetaKeys(vdf);

                // save the manifest file
                if (updated)
                {
                    var vdfSerializer = new VdfSerializer(vdf);
                    vdfSerializer.Serialize(manifestFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_BranchClearErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal static ServerProfile FromDefaults()
        {
            var settings = new ServerProfile();
            settings.LoadServerFiles(true, true);
            settings.SetupServerFilesWatcher();
            return settings;
        }

        private void GetDefaultDirectories()
        {
            if (!string.IsNullOrWhiteSpace(InstallDirectory))
                return;

            // get the root servers folder
            var installDirectory = Path.IsPathRooted(Config.Default.ServersInstallPath)
                                       ? Path.Combine(Config.Default.ServersInstallPath)
                                       : Path.Combine(Config.Default.DataPath, Config.Default.ServersInstallPath);
            var index = 1;
            while (true)
            {
                // create a test profile folder name
                var profileFolder = $"{Config.Default.DefaultServerRelativePath}{index}";
                // get the test profile directory
                var profileDirectory = Path.Combine(installDirectory, profileFolder);

                // check if the profile directory exists
                if (!Directory.Exists(profileDirectory))
                {
                    // profile directory does not exist, assign the test profile directory to the profile
                    InstallDirectory = profileDirectory;
                    break;
                }

                index++;
            }
        }

        private static IEnumerable<Enum> GetExclusions()
        {
            return new List<Enum>();
        }

        public string GetLauncherFile() => Path.Combine(GetProfileServerConfigDir(), Config.Default.LauncherFile);

        [Obsolete("This method will be removed in a future version.")]
        public string GetProfileConfigDir_Old() => Path.Combine(Path.GetDirectoryName(GetProfileFile()), this.ProfileName);

        public string GetProfileFile() => Path.Combine(Config.Default.ConfigPath, Path.ChangeExtension(this.ProfileID.ToLower(), Config.Default.ProfileExtension));

        public string GetProfileKey() => TaskSchedulerUtils.ComputeKey(this.InstallDirectory);

        public string GetProfileServerConfigDir() => GetProfileServerConfigDir(this);

        public static string GetProfileServerConfigDir(ServerProfile profile) => Path.Combine(profile.InstallDirectory, Config.Default.ServerConfigRelativePath);

        public string GetServerAppId()
        {
            try
            {
                var appFile = Path.Combine(InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerAppIdFile);
                return File.Exists(appFile) ? File.ReadAllText(appFile).Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public string GetServerArgs()
        {
            var serverArgs = new StringBuilder();

            serverArgs.Append(GetProfileMapName(this));

            if (!string.IsNullOrWhiteSpace(this.ServerIP))
            {
                serverArgs.Append(" ");
                serverArgs.AppendFormat(Config.Default.ServerCommandLineArgsIPMatchFormat, this.ServerIP);
            }
            serverArgs.Append($" -Port={this.ServerPort}");
            serverArgs.Append(" ");
            serverArgs.AppendFormat(Config.Default.ServerCommandLineArgsMatchFormat, this.QueryPort);
            if (this.RconEnabled)
            {
                serverArgs.AppendFormat($" -RconPort={this.RconPort}");
            }

            if (!string.IsNullOrWhiteSpace(this.AdditionalArgs))
            {
                serverArgs.Append($" {this.AdditionalArgs.Trim()}");
            }

            serverArgs.Append(" ");
            serverArgs.Append(Config.Default.ServerCommandLineStandardArgs);

            if (this.OutputServerLog)
            {
                serverArgs.Append($" -log");
            }

            return serverArgs.ToString();
        }

        public string GetServerExeFile() => Path.Combine(this.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExeFile);

        public string GetServerSavedFilePath() => Path.Combine(this.InstallDirectory, Config.Default.SavedFilesRelativePath);

        public string GetServerWorldFile()
        {
            var saveFolder = GetServerSavedFilePath();
            return IOUtils.NormalizePath(Path.Combine(saveFolder, ServerMapSaveFileName));
        }

        public static ServerProfile LoadFrom(string file, ServerProfile profile = null)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            if (Path.GetExtension(file) == Config.Default.ProfileExtension)
                return LoadFromProfileFile(file, profile);

            var filePath = Path.GetDirectoryName(file);
            profile = LoadFromConfigFiles(file, profile, exclusions: null);

            if (filePath.EndsWith(Config.Default.ServerConfigRelativePath))
            {
                var installDirectory = filePath.Replace(Config.Default.ServerConfigRelativePath, string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                profile.ChangeInstallationFolder(installDirectory, reloadConfigFiles: false);
            }

            return profile;
        }

        public static ServerProfile LoadFromConfigFiles(string file, ServerProfile profile, IEnumerable<Enum> exclusions = null)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            if (exclusions == null)
                exclusions = GetExclusions();

            var iniPath = Path.GetDirectoryName(file);
            var iniFile = new SystemIniFile(iniPath);
            profile = profile ?? new ServerProfile();
            iniFile.Deserialize(profile, exclusions);

            if (string.IsNullOrWhiteSpace(profile.ServerModIds) && iniPath.EndsWith(Config.Default.ServerConfigRelativePath))
            {
                var installDirectory = iniPath.Replace(Config.Default.ServerConfigRelativePath, string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var modIds = ModUtils.ReadModListFile(installDirectory);
                profile.ServerModIds = string.Join(",", modIds);
            }

            return profile;
        }

        public static ServerProfile LoadFromProfileFile(string file, ServerProfile profile)
        {
            profile = LoadFromProfileFileBasic(file, profile);

            if (profile is null)
                return null;

            profile.CheckLauncherArgs();

            profile.LoadServerFiles(true, true);
            profile.SetupServerFilesWatcher();

            profile._lastSaveLocation = file;
            return profile;
        }

        public static ServerProfile LoadFromProfileFileBasic(string file, ServerProfile profile)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            if (Path.GetExtension(file) != Config.Default.ProfileExtension)
                return null;

            profile = profile ?? new ServerProfile();

            try
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new NullableValueConverter<int>());
                settings.Converters.Add(new NullableValueConverter<float>());

                profile = JsonUtils.DeserializeFromFile<ServerProfile>(file, settings);
                if (profile == null)
                    return null;
            }
            catch
            {
                // could not load the profile file, just exit
                return null;
            }

            // check if profile id and filename match
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (Guid.TryParse(fileName, out Guid fileId) && Guid.TryParse(profile.ProfileID, out Guid profileId))
            {
                // filename is a guid - check it against the profile id
                if (!Guid.Equals(fileId, profileId))
                {
                    // id values are not in sync, change the profile id to be the same as the filename
                    profile.ProfileID = fileId.ToString();
                }
            }

            var serverConfigFile = Path.Combine(GetProfileServerConfigDir(profile), Config.Default.ServerGameConfigFile);
            if (File.Exists(serverConfigFile))
            {
                profile = LoadFromConfigFiles(serverConfigFile, profile);
            }

            profile._lastSaveLocation = file;
            return profile;
        }

        public void Save(bool updateFolderPermissions, bool updateSchedules, ProgressDelegate progressCallback)
        {
            if (string.IsNullOrWhiteSpace(Config.Default.DataPath))
                return;

            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_Saving"));

            UpdateProfileToolTip();
            ClearSteamAppManifestBranch();
            CheckLauncherArgs();

            // check if the processor affinity is valid
            if (!ProcessUtils.IsProcessorAffinityValid(ProcessAffinity))
                // processor affinity is not valid, set back to 0 (default ALL processors)
                ProcessAffinity = 0;

            //
            // Save the profile
            //
            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_SavingProfileFile"));
            SaveProfile();

            try
            {
                DestroyServerFilesWatcher();

                SaveServerFileBlacklisted();
                SaveServerFileWhitelisted();
            }
            finally
            {
                SetupServerFilesWatcher();
            }

            //
            // Write the config files
            //
            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_SavingConfigFiles"));
            SaveConfigFiles();

            // update the modlist file
            ModUtils.CreateModListFile(InstallDirectory, ModUtils.GetModIdList(ServerModIds));

            //
            // If this was a rename, remove the old profile after writing the new one.
            //
            if (!String.Equals(GetProfileFile(), this._lastSaveLocation, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (File.Exists(this._lastSaveLocation))
                        File.Delete(this._lastSaveLocation);

                    var profileFile = Path.ChangeExtension(this._lastSaveLocation, Config.Default.ProfileExtension);
                    if (File.Exists(profileFile))
                        File.Delete(profileFile);

                    var profileIniDir = Path.ChangeExtension(this._lastSaveLocation, null);
                    if (Directory.Exists(profileIniDir))
                        Directory.Delete(profileIniDir, true);
                }
                catch (IOException)
                {
                    // We tried...
                }

                this._lastSaveLocation = GetProfileFile();
            }

            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_SavingLauncherFile"));
            SaveLauncher();

            if (updateFolderPermissions)
            {
                progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_CheckingDirectoryPermissions"));
                UpdateDirectoryPermissions();
            }

            if (updateSchedules)
            {
                progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_CheckingScheduledTasks"));
                UpdateSchedules();
            }
        }

        public void SaveProfile()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new NullableValueConverter<int>());
            settings.Converters.Add(new NullableValueConverter<float>());

            //
            // Save the profile
            //
            JsonUtils.SerializeToFile(this, GetProfileFile(), settings);
        }

        public void SaveLauncher()
        {
            var commandArgs = new StringBuilder();

            if (this.LauncherArgsOverride)
            {
                commandArgs.Append(this.LauncherArgs);
            }
            else
            {
                if (this.LauncherArgsPrefix && !string.IsNullOrWhiteSpace(this.LauncherArgs))
                {
                    commandArgs.AppendLine(this.LauncherArgs.Trim());
                }

                commandArgs.Append("start");
                commandArgs.Append($" \"{this.ProfileName}\"");

                if (Config.Default.ServerStartMinimized)
                {
                    commandArgs.Append($" /min");
                }

                commandArgs.Append($" /{ProcessPriority}");
                if (ProcessAffinity > 0 && ProcessUtils.IsProcessorAffinityValid(ProcessAffinity))
                {
                    commandArgs.Append($" /affinity {ProcessAffinity:X}");
                }

                if (!this.LauncherArgsPrefix && !string.IsNullOrWhiteSpace(this.LauncherArgs))
                {
                    commandArgs.Append($" {this.LauncherArgs.Trim()}");
                }

                commandArgs.Append($" \"{GetServerExeFile()}\"");
                commandArgs.Append($" {GetServerArgs()}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(GetLauncherFile()));
            File.WriteAllText(GetLauncherFile(), commandArgs.ToString());
        }

        public void SaveConfigFiles()
        {
            //
            // Remove the old profile ini backups
            //
            var profileIniDir = GetProfileConfigDir_Old();
            if (Directory.Exists(profileIniDir))
            {
                // set the folder permissions
                SecurityUtils.SetDirectoryOwnershipForAllUsers(profileIniDir);
                try
                {
                    Directory.Delete(profileIniDir, true);
                }
                catch (Exception) { }
            }

            //
            // Save to the installation location
            //
            string serverConfigDir = GetProfileServerConfigDir();
            Directory.CreateDirectory(serverConfigDir);
            SaveConfigFile(serverConfigDir);
        }

        public void SaveConfigFile(string configDir, IEnumerable<Enum> exclusions = null)
        {
            if (exclusions == null)
                exclusions = GetExclusions();

            var iniFile = new SystemIniFile(configDir);
            iniFile.Serialize(this, exclusions);
        }

        public bool UpdateDirectoryPermissions()
        {
            if (!SecurityUtils.IsAdministrator())
                return true;

            if (!SecurityUtils.SetDirectoryOwnershipForAllUsers(this.InstallDirectory))
            {
                Logger.Debug($"Unable to set directory permissions for {this.InstallDirectory}.");
                return false;
            }

            return true;
        }

        public void UpdatePortsString()
        {
            this.PortsString = $"{this.ServerPort}, {this.ServerPeerPort}, {this.QueryPort}";
        }

        public bool UpdateSchedules()
        {
            SaveLauncher();

            if (!SecurityUtils.IsAdministrator())
                return true;

            var taskKey = GetProfileKey();

            if (!TaskSchedulerUtils.ScheduleAutoStart(taskKey, null, this.EnableAutoStart, GetLauncherFile(), ProfileName, this.AutoStartOnLogin, Config.Default.TaskSchedulerUsername, Config.Default.TaskSchedulerPassword, Config.Default.AutoStart_TaskPriority))
            {
                return false;
            }

            TimeSpan shutdownTime;
            var command = Assembly.GetEntryAssembly().Location;
            if (!TaskSchedulerUtils.ScheduleAutoShutdown(taskKey, "#1", command, this.EnableAutoShutdown1 ? (TimeSpan.TryParseExact(this.AutoShutdownTime1, "g", null, out shutdownTime) ? shutdownTime : (TimeSpan?)null) : null, ShutdownDaysOfTheWeek1, ProfileName, TaskSchedulerUtils.ShutdownType.Shutdown1, Config.Default.AutoShutdown_TaskPriority))
            {
                return false;
            }

            if (!TaskSchedulerUtils.ScheduleAutoShutdown(taskKey, "#2", command, this.EnableAutoShutdown2 ? (TimeSpan.TryParseExact(this.AutoShutdownTime2, "g", null, out shutdownTime) ? shutdownTime : (TimeSpan?)null) : null, ShutdownDaysOfTheWeek2, ProfileName, TaskSchedulerUtils.ShutdownType.Shutdown2, Config.Default.AutoShutdown_TaskPriority))
            {
                return false;
            }

            return true;
        }

        public bool Validate(bool forceValidate, out string validationMessage)
        {
            validationMessage = string.Empty;
            StringBuilder result = new StringBuilder();

            var appId = UseTestlive ? Config.Default.AppId_Testlive : Config.Default.AppId;

            // checking the port values are within the valid range
            if (ServerPort < ushort.MinValue || ServerPort > ushort.MaxValue)
            {
                var message = _globalizer.GetResourceString("ProfileValidation_ServerPort")?.Replace("{PortMinimum}", ushort.MinValue.ToString()).Replace("{PortMaximum}", ushort.MaxValue.ToString());
                result.AppendLine(message);
            }
            if (ServerPeerPort < ushort.MinValue || ServerPeerPort > ushort.MaxValue)
            {
                var message = _globalizer.GetResourceString("ProfileValidation_ServerPeerPort")?.Replace("{PortMinimum}", ushort.MinValue.ToString()).Replace("{PortMaximum}", ushort.MaxValue.ToString());
                result.AppendLine(message);
            }
            if (QueryPort < ushort.MinValue || QueryPort > ushort.MaxValue)
            {
                var message = _globalizer.GetResourceString("ProfileValidation_QueryPort")?.Replace("{PortMinimum}", ushort.MinValue.ToString()).Replace("{PortMaximum}", ushort.MaxValue.ToString());
                result.AppendLine(message);
            }
            if (RconPort < ushort.MinValue || RconPort > ushort.MaxValue)
            {
                var message = _globalizer.GetResourceString("ProfileValidation_RconPort")?.Replace("{PortMinimum}", ushort.MinValue.ToString()).Replace("{PortMaximum}", ushort.MaxValue.ToString());
                result.AppendLine(message);
            }

            if (forceValidate || Config.Default.ValidateProfileOnServerStart)
            {
                // build a list of mods to be processed
                var modIds = ModUtils.GetModIdList(ServerModIds);
                modIds = ModUtils.ValidateModList(modIds);

                var modIdList = new List<string>();
                modIdList.AddRange(modIds);

                modIdList = ModUtils.ValidateModList(modIdList);

                var modDetails = SteamUtils.GetSteamModDetails(modIdList);

                // check for map name.
                if (string.IsNullOrWhiteSpace(ServerMap))
                {
                    var message = _globalizer.GetResourceString("ProfileValidation_MapNameBlank");
                    result.AppendLine(message);
                }

                // check if the server executable exists
                var serverFolder = Path.Combine(InstallDirectory, Config.Default.ServerBinaryRelativePath);
                var serverFile = Path.Combine(serverFolder, Config.Default.ServerExeFile);
                if (!Directory.Exists(serverFolder))
                {
                    var message = _globalizer.GetResourceString("ProfileValidation_ServerNotDownloaded");
                    result.AppendLine(message);
                }
                else if (!File.Exists(serverFile))
                {
                    var message = _globalizer.GetResourceString("ProfileValidation_ServerExeNotDownloaded")?.Replace("{ServerExe}", Config.Default.ServerExeFile);
                    result.AppendLine(message);
                }
                else
                {
                    var serverAppId = GetServerAppId();
                    if (!serverAppId.Equals(appId))
                    {
                        var message = _globalizer.GetResourceString("ProfileValidation_ServerDifferentApplication");
                        result.AppendLine(message);
                    }
                }

                // check for the mods
                foreach (var modId in modIds)
                {
                    var modFilename = $"{modId}.pak";
                    var modFile = IOUtils.NormalizePath(Path.Combine(ModUtils.GetModRootPath(InstallDirectory), modFilename));
                    if (!File.Exists(modFile))
                    {
                        var message = _globalizer.GetResourceString("ProfileValidation_ModFileNotDownloaded")?.Replace("{modId}", modId);
                        result.AppendLine(message);
                    }
                    else
                    {
                        var modDetail = modDetails?.publishedfiledetails?.FirstOrDefault(d => d.publishedfileid.Equals(modId));
                        if (modDetail != null && modDetail.consumer_app_id != null)
                        {
                            if (!modDetail.consumer_app_id.Equals(appId))
                            {
                                var message = _globalizer.GetResourceString("ProfileValidation_ModDifferentApplication")?.Replace("modId", modId);
                                result.AppendLine(message);
                            }
                            else
                            {
                                var modVersion = ModUtils.GetModLatestTime(ModUtils.GetLatestModTimeFile(InstallDirectory, modId));
                                if (modVersion == 0 || !modVersion.Equals(modDetail.time_updated))
                                {
                                    var message = _globalizer.GetResourceString("ProfileValidation_ModOutdated")?.Replace("{modId}", modId);
                                    result.AppendLine(message);
                                }
                            }
                        }
                        else
                        {
                            var message = _globalizer.GetResourceString("ProfileValidation_ModSteamError")?.Replace("{modId}", modId);
                            result.AppendLine(message);
                        }
                    }
                }
            }

            validationMessage = result.ToString();
            return string.IsNullOrWhiteSpace(validationMessage);
        }

        public string ToOutputString()
        {
            //
            // serializes the profile to a string
            //
            return JsonUtils.Serialize<ServerProfile>(this);
        }

        public int RestoreSaveFiles(string restoreFile, bool isArchiveFile, bool restoreAll)
        {
            if (string.IsNullOrWhiteSpace(restoreFile) || !File.Exists(restoreFile))
            {
                var message = _globalizer.GetResourceString("RestoreSaveFiles_BackupFileNotFound");
                throw new FileNotFoundException(message, restoreFile);
            }

            var saveFolder = GetProfileSavePath(this);
            if (!Directory.Exists(saveFolder))
            {
                var message = _globalizer.GetResourceString("RestoreSaveFiles_SaveFolderNotFound")?.Replace("{saveFolder}", saveFolder);
                throw new DirectoryNotFoundException(message);
            }

            var worldFileName = ServerMapSaveFileName;
            var saveGamesFolder = GetProfileSaveGamesPath(this);

            // check if the archive file contains the world save file at minimum
            if (isArchiveFile)
            {
                if (!ZipUtils.DoesFileExist(restoreFile, worldFileName))
                {
                    var message = _globalizer.GetResourceString("RestoreSaveFiles_MissingWorldSaveFile");
                    throw new Exception(message);
                }
            }

            // create a backup of the existing save folder
            var app = new ServerApp(true)
            {
                BackupWorldFile = false,
                DeleteOldBackupFiles = false,
                SendAlerts = false,
                SendEmails = false,
                OutputLogs = false
            };
            app.CreateServerBackupArchiveFile(null, ServerProfileSnapshot.Create(this));

            var worldFile = IOUtils.NormalizePath(Path.Combine(saveFolder, worldFileName));
            var restoreFileInfo = new FileInfo(restoreFile);
            var restoredFileCount = 0;

            if (isArchiveFile)
            {
                // create a list of files to be deleted
                var directories = new List<string>();
                var files = new List<string>
                {
                    worldFile,
                };

                // add the world save support files
                files.AddRange(Directory.GetFiles(saveFolder, $"{worldFileName}-*"));

                if (restoreAll)
                {
                    if (Directory.Exists(saveGamesFolder) && ZipUtils.DoesFolderExist(restoreFile, Config.Default.SaveGamesRelativePath))
                    {
                        directories.Add(saveGamesFolder);
                    }
                }

                // delete the selected files
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // if unable to delete, do not bother
                    }
                }

                foreach (var directory in directories)
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch
                    {
                        // if unable to delete, do not bother
                    }
                }

                if (restoreAll)
                {
                    // restore the files from the backup
                    restoredFileCount += ZipUtils.ExtractFiles(restoreFile, saveFolder, sourceFolder: "", recurseFolders: false);

                    if (ZipUtils.DoesFolderExist(restoreFile, Config.Default.SaveGamesRelativePath))
                    {
                        var rootSaveFolder = Path.GetDirectoryName(saveGamesFolder);
                        restoredFileCount += ZipUtils.ExtractFiles(restoreFile, rootSaveFolder, Config.Default.SaveGamesRelativePath, recurseFolders: true);
                    }
                }
                else
                {
                    restoredFileCount += ZipUtils.ExtractAFile(restoreFile, worldFileName, saveFolder);
                }
            }
            else
            {
                // copy the selected file
                File.Copy(restoreFile, worldFile, true);
                File.SetCreationTime(worldFile, restoreFileInfo.CreationTime);
                File.SetLastWriteTime(worldFile, restoreFileInfo.LastWriteTime);
                File.SetLastAccessTime(worldFile, restoreFileInfo.LastAccessTime);

                restoredFileCount = 1;
            }

            return restoredFileCount;
        }

        public void UpdateProfileToolTip()
        {
            ProfileToolTip = $"{ProfileName ?? string.Empty} ({ProfileID ?? string.Empty})";
        }

        public void ValidateServerName()
        {
            ServerNameLength = Encoding.UTF8.GetByteCount(ServerName);
            ServerNameLengthToLong = ServerNameLength > 48;
        }

        public void ValidateMOTD()
        {
            MOTDLength = Encoding.UTF8.GetByteCount(MOTD);
            MOTDLengthToLong = MOTDLength > 200;

            MOTDLineCount = string.IsNullOrWhiteSpace(MOTD) ? 0 : MOTD.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
            MOTDLineCountToLong = MOTDLineCount > 5;
        }

        #endregion

        #region Reset Methods
        public void ResetProfileId()
        {
            this.ProfileID = Guid.NewGuid().ToString();
        }

        public void ResetRconWindowExtents()
        {
            this.ClearValue(RconWindowExtentsProperty);
        }

        public void ResetServerOptions()
        {
            this.ClearValue(UseVACProperty);
            this.ClearValue(UseBattlEyeProperty);
            this.ClearValue(AllowFamilySharedAccountProperty);
            this.ClearValue(ServerRegionProperty);
            this.ClearValue(MaxPlayersProperty);

            this.ClearValue(KickIdlePlayersPercentageProperty);
            this.ClearValue(KickIdlePlayersPeriodProperty);

            this.ClearValue(ServerTransferEnabledProperty);
            this.ClearValue(CanImportDirectlyFromSameServerProperty);
            this.ClearValue(OutputServerLogProperty);
        }

        // section reset methods
        public void ResetAdministrationSection()
        {
            this.ClearValue(ServerNameProperty);
            this.ClearValue(ServerPasswordProperty);
            this.ClearValue(AdminPasswordProperty);

            this.ClearValue(ServerIPProperty);
            this.ClearValue(ServerPortProperty);
            this.ClearValue(ServerPeerPortProperty);
            this.ClearValue(QueryPortProperty);

            UpdatePortsString();

            this.ClearValue(ServerMapProperty);
            this.ClearValue(ServerMapSaveFileNameProperty);
            this.ClearValue(ServerModIdsProperty);

            this.ClearValue(RconEnabledProperty);
            this.ClearValue(RconPortProperty);
            this.ClearValue(RconPasswordProperty);

            this.ClearValue(MOTDProperty);
            this.ClearValue(MOTDIntervalEnabledProperty);
            this.ClearValue(MOTDIntervalProperty);

            ResetServerOptions();

            this.ClearValue(ProcessPriorityProperty);
            this.ClearValue(ProcessAffinityProperty);

            this.ClearValue(LauncherArgsProperty);
            this.ClearValue(LauncherArgsOverrideProperty);
            this.ClearValue(LauncherArgsPrefixProperty);
            this.ClearValue(AdditionalArgsProperty);
        }

        public void ResetDiscordBotSection()
        {
            this.ClearValue(DiscordChannelIdProperty);
            this.ClearValue(DiscordAliasProperty);

            this.ClearValue(AllowDiscordBackupProperty);
            this.ClearValue(AllowDiscordRestartProperty);
            this.ClearValue(AllowDiscordShutdownProperty);
            this.ClearValue(AllowDiscordStartProperty);
            this.ClearValue(AllowDiscordStopProperty);
            this.ClearValue(AllowDiscordUpdateProperty);
        }

        public void ResetServerDetailsSection()
        {
            this.ClearValue(UseTestliveProperty);
            this.ClearValue(BranchNameProperty);
            this.ClearValue(BranchPasswordProperty);
        }
        #endregion

        #region Sync Methods
        public void SyncSettings(ServerProfileCategory category, ServerProfile sourceProfile)
        {
            if (sourceProfile == null)
                return;

            switch (category)
            {
                case ServerProfileCategory.Administration:
                    SyncAdministrationSection(sourceProfile);
                    break;
                case ServerProfileCategory.AutomaticManagement:
                    SyncAutomaticManagement(sourceProfile);
                    break;
                case ServerProfileCategory.DiscordBot:
                    SyncDiscordBot(sourceProfile);
                    break;
                case ServerProfileCategory.ServerDetails:
                    SyncServerDetails(sourceProfile);
                    break;
                case ServerProfileCategory.ServerFiles:
                    SyncServerFiles(sourceProfile);
                    break;
            }
        }

        private void SyncAdministrationSection(ServerProfile sourceProfile)
        {
            this.SetValue(ServerModIdsProperty, sourceProfile.ServerModIds);

            this.SetValue(UseVACProperty, sourceProfile.UseVAC);
            this.SetValue(UseBattlEyeProperty, sourceProfile.UseBattlEye);
            this.SetValue(AllowFamilySharedAccountProperty, sourceProfile.AllowFamilySharedAccount);
            this.SetValue(ServerRegionProperty, sourceProfile.ServerRegion);
            this.SetValue(MaxPlayersProperty, sourceProfile.MaxPlayers);

            this.SetValue(KickIdlePlayersPercentageProperty, sourceProfile.KickIdlePlayersPercentage);
            this.SetValue(KickIdlePlayersPeriodProperty, sourceProfile.KickIdlePlayersPeriod);

            this.SetValue(ServerTransferEnabledProperty, sourceProfile.ServerTransferEnabled);
            this.SetValue(CanImportDirectlyFromSameServerProperty, sourceProfile.CanImportDirectlyFromSameServer);
            this.SetValue(OutputServerLogProperty, sourceProfile.OutputServerLog);

            this.SetValue(LauncherArgsProperty, sourceProfile.LauncherArgs);
            this.SetValue(LauncherArgsOverrideProperty, sourceProfile.LauncherArgsOverride);
            this.SetValue(LauncherArgsPrefixProperty, sourceProfile.LauncherArgsPrefix);
            this.SetValue(AdditionalArgsProperty, sourceProfile.AdditionalArgs);
        }

        private void SyncAutomaticManagement(ServerProfile sourceProfile)
        {
            this.SetValue(EnableAutoStartProperty, sourceProfile.EnableAutoStart);
            this.SetValue(AutoStartOnLoginProperty, sourceProfile.AutoStartOnLogin);

            this.SetValue(EnableAutoShutdown1Property, sourceProfile.EnableAutoShutdown1);
            this.SetValue(AutoShutdownTime1Property, sourceProfile.AutoShutdownTime1);
            this.SetValue(ShutdownDaysOfTheWeek1Property, sourceProfile.ShutdownDaysOfTheWeek1);
            this.SetValue(RestartAfterShutdown1Property, sourceProfile.RestartAfterShutdown1);
            this.SetValue(UpdateAfterShutdown1Property, sourceProfile.UpdateAfterShutdown1);

            this.SetValue(EnableAutoShutdown2Property, sourceProfile.EnableAutoShutdown2);
            this.SetValue(AutoShutdownTime2Property, sourceProfile.AutoShutdownTime2);
            this.SetValue(ShutdownDaysOfTheWeek2Property, sourceProfile.ShutdownDaysOfTheWeek2);
            this.SetValue(RestartAfterShutdown2Property, sourceProfile.RestartAfterShutdown2);
            this.SetValue(UpdateAfterShutdown2Property, sourceProfile.UpdateAfterShutdown2);

            this.SetValue(EnableAutoBackupProperty, sourceProfile.EnableAutoBackup);
            this.SetValue(EnableAutoUpdateProperty, sourceProfile.EnableAutoUpdate);
            this.SetValue(AutoRestartIfShutdownProperty, sourceProfile.AutoRestartIfShutdown);
        }

        private void SyncDiscordBot(ServerProfile sourceProfile)
        {
            this.SetValue(DiscordChannelIdProperty, sourceProfile.DiscordChannelId);
            this.SetValue(DiscordAliasProperty, sourceProfile.DiscordAlias);

            this.SetValue(AllowDiscordBackupProperty, sourceProfile.AllowDiscordBackup);
            this.SetValue(AllowDiscordRestartProperty, sourceProfile.AllowDiscordRestart);
            this.SetValue(AllowDiscordShutdownProperty, sourceProfile.AllowDiscordShutdown);
            this.SetValue(AllowDiscordStartProperty, sourceProfile.AllowDiscordStart);
            this.SetValue(AllowDiscordStopProperty, sourceProfile.AllowDiscordStop);
            this.SetValue(AllowDiscordUpdateProperty, sourceProfile.AllowDiscordUpdate);
        }

        private void SyncServerDetails(ServerProfile sourceProfile)
        {
            this.SetValue(UseTestliveProperty, sourceProfile.UseTestlive);
            this.SetValue(BranchNameProperty, sourceProfile.BranchName);
            this.SetValue(BranchPasswordProperty, sourceProfile.BranchPassword);
        }

        private void SyncServerFiles(ServerProfile sourceProfile)
        {
            this.SetValue(ServerFilesBlacklistedProperty, sourceProfile.ServerFilesBlacklisted);
            this.SetValue(EnableWhitelistProperty, sourceProfile.EnableWhitelist);
            this.SetValue(ServerFilesWhitelistedProperty, sourceProfile.ServerFilesWhitelisted);

            SaveServerFileBlacklisted();
            SaveServerFileWhitelisted();
        }

        public void SyncMapSaveFileName()
        {
            var mapSaveFileName = GameData.MapSaveNameForClass(ServerMap, true);
            if (!string.IsNullOrWhiteSpace(mapSaveFileName))
            {
                this.ServerMapSaveFileName = mapSaveFileName;
            }
        }
        #endregion

        #region Server Files 
        private void ServerFilesWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var blacklistFile = false;
            var whitelistFile = false;

            if (e.Name.Equals(Config.Default.ServerBlacklistFile, StringComparison.OrdinalIgnoreCase))
            {
                blacklistFile = true;
            }
            if (e.Name.Equals(Config.Default.ServerWhitelistFile, StringComparison.OrdinalIgnoreCase))
            {
                whitelistFile = true;
            }

            TaskUtils.RunOnUIThreadAsync(() => LoadServerFiles(blacklistFile, whitelistFile)).DoNotWait();
        }

        private void ServerFilesWatcher_Error(object sender, ErrorEventArgs e)
        {
            TaskUtils.RunOnUIThreadAsync(() => SetupServerFilesWatcher()).DoNotWait();
        }

        public void DestroyServerFilesWatcher()
        {
            if (_serverFilesWatcher == null)
                return;

            _serverFilesWatcher.EnableRaisingEvents = false;
            _serverFilesWatcher.Changed -= ServerFilesWatcher_Changed;
            _serverFilesWatcher.Created -= ServerFilesWatcher_Changed;
            _serverFilesWatcher.Deleted -= ServerFilesWatcher_Changed;
            _serverFilesWatcher.Error -= ServerFilesWatcher_Error;
            _serverFilesWatcher = null;
        }

        public void SetupServerFilesWatcher()
        {
            if (_serverFilesWatcher != null)
                DestroyServerFilesWatcher();

            if (!EnableServerFilesWatcher)
                return;

            var path = GetServerSavedFilePath();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return;

            _serverFilesWatcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite,
            };

            _serverFilesWatcher.Changed += ServerFilesWatcher_Changed;
            _serverFilesWatcher.Created += ServerFilesWatcher_Changed;
            _serverFilesWatcher.Deleted += ServerFilesWatcher_Changed;
            _serverFilesWatcher.Error += ServerFilesWatcher_Error;
            _serverFilesWatcher.EnableRaisingEvents = true;
        }

        public void LoadServerFiles(bool blacklistFile, bool whitelistFile)
        {
            try
            {
                var list1 = this.ServerFilesBlacklisted ?? new PlayerUserList();
                var list2 = this.ServerFilesWhitelisted ?? new PlayerUserList();

                var allSteamIds = new List<string>();
                string[] blacklistSteamIds = null;
                string[] whitelistSteamIds = null;

                if (blacklistFile)
                {
                    var file = Path.Combine(InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerBlacklistFile);
                    if (File.Exists(file))
                    {
                        blacklistSteamIds = File.ReadAllLines(file);
                        allSteamIds.AddRange(blacklistSteamIds);
                    }
                }

                if (whitelistFile)
                {
                    var file = Path.Combine(InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerWhitelistFile);
                    if (File.Exists(file))
                    {
                        whitelistSteamIds = File.ReadAllLines(file);
                        allSteamIds.AddRange(whitelistSteamIds);
                    }
                }

                // remove all duplicates
                allSteamIds = allSteamIds.Distinct().ToList();

                // fetch the details of all steam users in the list
                var steamUsers = SteamUtils.GetSteamUserDetails(allSteamIds);

                if (blacklistFile && blacklistSteamIds != null)
                {
                    list1 = PlayerUserList.GetList(steamUsers, blacklistSteamIds);
                }

                if (whitelistFile && whitelistSteamIds != null)
                {
                    list2 = PlayerUserList.GetList(steamUsers, whitelistSteamIds);
                }

                this.ServerFilesBlacklisted = list1;
                this.ServerFilesWhitelisted = list2;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ServerFilesLoadErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveServerFileBlacklisted()
        {
            try
            {
                var folder = Path.Combine(InstallDirectory, Config.Default.SavedFilesRelativePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var file = Path.Combine(folder, Config.Default.ServerBlacklistFile);
                File.WriteAllLines(file, this.ServerFilesBlacklisted.ToEnumerable());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ServerFilesSaveErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveServerFileWhitelisted()
        {
            try
            {
                var folder = Path.Combine(InstallDirectory, Config.Default.SavedFilesRelativePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var file = Path.Combine(folder, Config.Default.ServerWhitelistFile);
                File.WriteAllLines(file, this.ServerFilesWhitelisted.ToEnumerable());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ServerFilesSaveErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Static Profile Methods
        public static string GetProfileMapName(ServerProfile profile)
        {
            return GetProfileMapName(profile?.ServerMap);
        }

        public static string GetProfileMapName(string serverMap)
        {
            return ModUtils.GetMapName(serverMap);
        }

        public static string GetProfileSaveGamesPath(ServerProfile profile)
        {
            return GetProfileSaveGamesPath(profile?.InstallDirectory);
        }

        public static string GetProfileSaveGamesPath(string installDirectory)
        {
            return Path.Combine(installDirectory ?? string.Empty, Config.Default.SavedRelativePath, Config.Default.SaveGamesRelativePath);
        }

        public static string GetProfileSavePath(ServerProfile profile)
        {
            return GetProfileSavePath(profile?.InstallDirectory);
        }

        public static string GetProfileSavePath(string installDirectory)
        {
            return Path.Combine(installDirectory ?? string.Empty, Config.Default.SavedFilesRelativePath);
        }
        #endregion

        #endregion
    }
}
