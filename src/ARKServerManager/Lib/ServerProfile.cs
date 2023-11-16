using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using NeXt.Vdf;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Interfaces;
using ServerManagerTool.Common.JsonConverters;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib.Model;
using ServerManagerTool.Lib.ViewModel;
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
using TinyCsvParser;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class ServerProfile : DependencyObject
    {
        private const char CSV_DELIMITER = ';';

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _lastSaveLocation = string.Empty;
        private FileSystemWatcher _serverFilesWatcherSaved = null;
        private FileSystemWatcher _serverFilesWatcherBinary = null;

        private ServerProfile()
        {
            ResetProfileId();

            ServerPassword = SecurityUtils.GeneratePassword(16);
            AdminPassword = SecurityUtils.GeneratePassword(16);
            BranchPassword = string.Empty;
            SpectatorPassword = string.Empty;

            ServerFilesAdmins = new PlayerUserList();
            ServerFilesExclusive = new PlayerUserList();
            ServerFilesWhitelisted = new PlayerUserList();

            // initialise the nullable properties
            this.ClearNullableValue(KickIdlePlayersPeriodProperty);
            this.ClearNullableValue(MOTDIntervalProperty);
            this.ClearNullableValue(MaxTributeDinosProperty);
            this.ClearNullableValue(MaxTributeItemsProperty);
            this.ClearNullableValue(ItemStatClamps_GenericQualityProperty);
            this.ClearNullableValue(ItemStatClamps_ArmorProperty);
            this.ClearNullableValue(ItemStatClamps_MaxDurabilityProperty);
            this.ClearNullableValue(ItemStatClamps_WeaponDamagePercentProperty);
            this.ClearNullableValue(ItemStatClamps_WeaponClipAmmoProperty);
            this.ClearNullableValue(ItemStatClamps_HypothermalInsulationProperty);
            this.ClearNullableValue(ItemStatClamps_WeightProperty);
            this.ClearNullableValue(ItemStatClamps_HyperthermalInsulationProperty);
            this.ClearNullableValue(OverrideMaxExperiencePointsPlayerProperty);
            this.ClearNullableValue(OverrideMaxExperiencePointsDinoProperty);
            this.ClearNullableValue(ImprintlimitProperty);

            // initialise the complex properties
            this.DinoSpawnWeightMultipliers = new AggregateIniValueList<DinoSpawn>(nameof(DinoSpawnWeightMultipliers), GameData.GetDinoSpawns);
            this.PreventDinoTameClassNames = new StringIniValueList(nameof(PreventDinoTameClassNames), () => new string[0] );
            this.PreventBreedingForClassNames = new StringIniValueList(nameof(PreventBreedingForClassNames), () => new string[0]);
            this.NPCReplacements = new AggregateIniValueList<NPCReplacement>(nameof(NPCReplacements), GameData.GetNPCReplacements);
            this.TamedDinoClassDamageMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(TamedDinoClassDamageMultipliers), GameData.GetDinoMultipliers);
            this.TamedDinoClassResistanceMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(TamedDinoClassResistanceMultipliers), GameData.GetDinoMultipliers);
            this.DinoClassDamageMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(DinoClassDamageMultipliers), GameData.GetDinoMultipliers);
            this.DinoClassResistanceMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(DinoClassResistanceMultipliers), GameData.GetDinoMultipliers);
            this.DinoSettings = new DinoSettingsList(this.DinoSpawnWeightMultipliers, this.PreventDinoTameClassNames, this.PreventBreedingForClassNames, this.NPCReplacements, this.TamedDinoClassDamageMultipliers, this.TamedDinoClassResistanceMultipliers, this.DinoClassDamageMultipliers, this.DinoClassResistanceMultipliers);

            this.DinoLevels = new LevelList();
            this.PlayerLevels = new LevelList();
            this.PlayerBaseStatMultipliers = new StatsMultiplierFloatArray(nameof(PlayerBaseStatMultipliers), GameData.GetBaseStatMultipliers_Player, GameData.GetStatMultiplierInclusions_PlayerBase(), true);
            this.PerLevelStatsMultiplier_Player = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_Player), GameData.GetPerLevelStatsMultipliers_Player, GameData.GetStatMultiplierInclusions_PlayerPerLevel(), true);
            this.PerLevelStatsMultiplier_DinoWild = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoWild), GameData.GetPerLevelStatsMultipliers_DinoWild, GameData.GetStatMultiplierInclusions_DinoWildPerLevel(), true);
            this.PerLevelStatsMultiplier_DinoTamed = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoTamed), GameData.GetPerLevelStatsMultipliers_DinoTamed, GameData.GetStatMultiplierInclusions_DinoTamedPerLevel(), true);
            this.PerLevelStatsMultiplier_DinoTamed_Add = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoTamed_Add), GameData.GetPerLevelStatsMultipliers_DinoTamedAdd, GameData.GetStatMultiplierInclusions_DinoTamedAdd(), true);
            this.PerLevelStatsMultiplier_DinoTamed_Affinity = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity), GameData.GetPerLevelStatsMultipliers_DinoTamedAffinity, GameData.GetStatMultiplierInclusions_DinoTamedAffinity(), true);
            this.MutagenLevelBoost = new StatsMultiplierIntegerArray(nameof(MutagenLevelBoost), GameData.GetPerLevelMutagenLevelBoost_DinoWild, GameData.GetMutagenLevelBoostInclusions_DinoWild(), true);
            this.MutagenLevelBoost_Bred = new StatsMultiplierIntegerArray(nameof(MutagenLevelBoost_Bred), GameData.GetPerLevelMutagenLevelBoost_DinoTamed, GameData.GetMutagenLevelBoostInclusions_DinoTamed(), true);

            this.HarvestResourceItemAmountClassMultipliers = new ResourceClassMultiplierList(nameof(HarvestResourceItemAmountClassMultipliers), GameData.GetResourceMultipliers);

            this.OverrideNamedEngramEntries = new EngramEntryList(nameof(OverrideNamedEngramEntries));
            this.EngramEntryAutoUnlocks = new EngramAutoUnlockList(nameof(EngramEntryAutoUnlocks));
            this.EngramSettings = new EngramSettingsList(this.OverrideNamedEngramEntries, this.EngramEntryAutoUnlocks);

            this.ConfigOverrideItemCraftingCosts = new CraftingOverrideList(nameof(ConfigOverrideItemCraftingCosts));
            this.ConfigOverrideItemMaxQuantity = new StackSizeOverrideList(nameof(ConfigOverrideItemMaxQuantity));
            this.ConfigOverrideSupplyCrateItems = new SupplyCrateOverrideList(nameof(ConfigOverrideSupplyCrateItems));
            this.ExcludeItemIndices = new ExcludeItemIndicesOverrideList(nameof(ExcludeItemIndices));
            this.PreventTransferForClassNames = new PreventTransferOverrideList(nameof(PreventTransferForClassNames));

            this.ConfigAddNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(ConfigAddNPCSpawnEntriesContainer), NPCSpawnContainerType.Add);
            this.ConfigSubtractNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(ConfigSubtractNPCSpawnEntriesContainer), NPCSpawnContainerType.Subtract);
            this.ConfigOverrideNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(ConfigOverrideNPCSpawnEntriesContainer), NPCSpawnContainerType.Override);
            this.NPCSpawnSettings = new NPCSpawnSettingsList(this.ConfigAddNPCSpawnEntriesContainer, this.ConfigSubtractNPCSpawnEntriesContainer, this.ConfigOverrideNPCSpawnEntriesContainer);

            this.CustomGameUserSettings = new CustomList();
            this.CustomGameSettings = new CustomList();
            this.CustomEngineSettings = new CustomList();

            this.PGM_Terrain = new PGMTerrain();

            GetDefaultDirectories();
        }

        #region Properties
        public static bool EnableServerFilesWatcher { get; set; } = true;

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

        public static readonly DependencyProperty LastStartedProperty = DependencyProperty.Register(nameof(LastStarted), typeof(DateTime), typeof(ServerProfile), new PropertyMetadata(DateTime.MinValue));
        [DataMember]
        public DateTime LastStarted
        {
            get { return (DateTime)GetValue(LastStartedProperty); }
            set { SetValue(LastStartedProperty, value); }
        }

        public static readonly DependencyProperty SequenceProperty = DependencyProperty.Register(nameof(Sequence), typeof(int), typeof(ServerProfile), new PropertyMetadata(99));
        [DataMember]
        public int Sequence
        {
            get { return (int)GetValue(SequenceProperty); }
            set { SetValue(SequenceProperty, value); }
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
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_SessionSettings, ServerProfileCategory.Administration, "SessionName")]
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
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public string ServerPassword
        {
            get { return (string)GetValue(ServerPasswordProperty); }
            set { SetValue(ServerPasswordProperty, value); }
        }

        public static readonly DependencyProperty AdminPasswordProperty = DependencyProperty.Register(nameof(AdminPassword), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, "ServerAdminPassword")]
        public string AdminPassword
        {
            get { return (string)GetValue(AdminPasswordProperty); }
            set { SetValue(AdminPasswordProperty, value); }
        }

        public static readonly DependencyProperty SpectatorPasswordProperty = DependencyProperty.Register(nameof(SpectatorPassword), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public string SpectatorPassword
        {
            get { return (string)GetValue(SpectatorPasswordProperty); }
            set { SetValue(SpectatorPasswordProperty, value); }
        }

        public static readonly DependencyProperty ServerPortProperty = DependencyProperty.Register(nameof(ServerPort), typeof(int), typeof(ServerProfile), new PropertyMetadata(7777));
        [DataMember]
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_SessionSettings, ServerProfileCategory.Administration, "Port")]
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
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_SessionSettings, ServerProfileCategory.Administration)]
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

        public static readonly DependencyProperty ServerIPProperty = DependencyProperty.Register(nameof(ServerIP), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_SessionSettings, ServerProfileCategory.Administration, "MultiHome")]
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_MultiHome, ServerProfileCategory.Administration, "MultiHome", WriteBoolValueIfNonEmpty = true)]
        public string ServerIP
        {
            get { return (string)GetValue(ServerIPProperty); }
            set { SetValue(ServerIPProperty, value); }
        }

        public static readonly DependencyProperty EnableBanListURLProperty = DependencyProperty.Register(nameof(EnableBanListURL), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableBanListURL
        {
            get { return (bool)GetValue(EnableBanListURLProperty); }
            set { SetValue(EnableBanListURLProperty, value); }
        }

        public static readonly DependencyProperty BanListURLProperty = DependencyProperty.Register(nameof(BanListURL), typeof(string), typeof(ServerProfile), new PropertyMetadata("http://arkdedicated.com/banlist.txt"));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, ConditionedOn = nameof(EnableBanListURL), QuotedString = QuotedStringType.True)]
        public string BanListURL
        {
            get { return (string)GetValue(BanListURLProperty); }
            set { SetValue(BanListURLProperty, value); }
        }

        public static readonly DependencyProperty EnableCustomDynamicConfigUrlProperty = DependencyProperty.Register(nameof(EnableCustomDynamicConfigUrl), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableCustomDynamicConfigUrl
        {
            get { return (bool)GetValue(EnableCustomDynamicConfigUrlProperty); }
            set { SetValue(EnableCustomDynamicConfigUrlProperty, value); }
        }

        public static readonly DependencyProperty CustomDynamicConfigUrlProperty = DependencyProperty.Register(nameof(CustomDynamicConfigUrl), typeof(string), typeof(ServerProfile), new PropertyMetadata(""));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, ConditionedOn = nameof(EnableCustomDynamicConfigUrl), QuotedString = QuotedStringType.True)]
        public string CustomDynamicConfigUrl
        {
            get { return (string)GetValue(CustomDynamicConfigUrlProperty); }
            set { SetValue(CustomDynamicConfigUrlProperty, value); }
        }

        public static readonly DependencyProperty EnableCustomLiveTuningUrlProperty = DependencyProperty.Register(nameof(EnableCustomLiveTuningUrl), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableCustomLiveTuningUrl
        {
            get { return (bool)GetValue(EnableCustomLiveTuningUrlProperty); }
            set { SetValue(EnableCustomLiveTuningUrlProperty, value); }
        }

        public static readonly DependencyProperty CustomLiveTuningUrlProperty = DependencyProperty.Register(nameof(CustomLiveTuningUrl), typeof(string), typeof(ServerProfile), new PropertyMetadata(""));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, ConditionedOn = nameof(EnableCustomLiveTuningUrl), QuotedString = QuotedStringType.True)]
        public string CustomLiveTuningUrl
        {
            get { return (string)GetValue(CustomLiveTuningUrlProperty); }
            set { SetValue(CustomLiveTuningUrlProperty, value); }
        }

        public static readonly DependencyProperty MaxPlayersProperty = DependencyProperty.Register(nameof(MaxPlayers), typeof(int), typeof(ServerProfile), new PropertyMetadata(70));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_GameSession, ServerProfileCategory.Administration)]
        public int MaxPlayers
        {
            get { return (int)GetValue(MaxPlayersProperty); }
            set { SetValue(MaxPlayersProperty, value); }
        }

        public static readonly DependencyProperty KickIdlePlayersPeriodProperty = DependencyProperty.Register(nameof(KickIdlePlayersPeriod), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 3600)));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public NullableValue<int> KickIdlePlayersPeriod
        {
            get { return (NullableValue<int>)GetValue(KickIdlePlayersPeriodProperty); }
            set { SetValue(KickIdlePlayersPeriodProperty, value); }
        }

        public static readonly DependencyProperty RCONEnabledProperty = DependencyProperty.Register(nameof(RCONEnabled), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public bool RCONEnabled
        {
            get { return (bool)GetValue(RCONEnabledProperty); }
            set { SetValue(RCONEnabledProperty, value); }
        }

        public static readonly DependencyProperty RCONPortProperty = DependencyProperty.Register(nameof(RCONPort), typeof(int), typeof(ServerProfile), new PropertyMetadata(32330));
        [DataMember]
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public int RCONPort
        {
            get { return (int)GetValue(RCONPortProperty); }
            set { SetValue(RCONPortProperty, value); }
        }

        public static readonly DependencyProperty RCONServerGameLogBufferProperty = DependencyProperty.Register(nameof(RCONServerGameLogBuffer), typeof(int), typeof(ServerProfile), new PropertyMetadata(600));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public int RCONServerGameLogBuffer
        {
            get { return (int)GetValue(RCONServerGameLogBufferProperty); }
            set { SetValue(RCONServerGameLogBufferProperty, value); }
        }

        public static readonly DependencyProperty AdminLoggingProperty = DependencyProperty.Register(nameof(AdminLogging), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public bool AdminLogging
        {
            get { return (bool)GetValue(AdminLoggingProperty); }
            set { SetValue(AdminLoggingProperty, value); }
        }

        public static readonly DependencyProperty ServerMapProperty = DependencyProperty.Register(nameof(ServerMap), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string ServerMap
        {
            get { return (string)GetValue(ServerMapProperty); }
            set { SetValue(ServerMapProperty, value); }
        }

        public static readonly DependencyProperty TotalConversionModIdProperty = DependencyProperty.Register(nameof(TotalConversionModId), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string TotalConversionModId
        {
            get { return (string)GetValue(TotalConversionModIdProperty); }
            set { SetValue(TotalConversionModIdProperty, value); }
        }

        public static readonly DependencyProperty ServerModIdsProperty = DependencyProperty.Register(nameof(ServerModIds), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, "ActiveMods")]
        public string ServerModIds
        {
            get { return (string)GetValue(ServerModIdsProperty); }
            set { SetValue(ServerModIdsProperty, value); }
        }

        public static readonly DependencyProperty EnableExtinctionEventProperty = DependencyProperty.Register(nameof(EnableExtinctionEvent), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool EnableExtinctionEvent
        {
            get { return (bool)GetValue(EnableExtinctionEventProperty); }
            set { SetValue(EnableExtinctionEventProperty, value); }
        }

        public static readonly DependencyProperty ExtinctionEventTimeIntervalProperty = DependencyProperty.Register(nameof(ExtinctionEventTimeInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(2592000));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, ConditionedOn = nameof(EnableExtinctionEvent))]
        public int ExtinctionEventTimeInterval
        {
            get { return (int)GetValue(ExtinctionEventTimeIntervalProperty); }
            set { SetValue(ExtinctionEventTimeIntervalProperty, value); }
        }

        public static readonly DependencyProperty ExtinctionEventUTCProperty = DependencyProperty.Register(nameof(ExtinctionEventUTC), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Administration, "NextExtinctionEventUTC", ClearWhenOff = nameof(EnableExtinctionEvent))]
        public int ExtinctionEventUTC
        {
            get { return (int)GetValue(ExtinctionEventUTCProperty); }
            set { SetValue(ExtinctionEventUTCProperty, value); }
        }

        public static readonly DependencyProperty AutoSavePeriodMinutesProperty = DependencyProperty.Register(nameof(AutoSavePeriodMinutes), typeof(float), typeof(ServerProfile), new PropertyMetadata(15.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public float AutoSavePeriodMinutes
        {
            get { return (float)GetValue(AutoSavePeriodMinutesProperty); }
            set { SetValue(AutoSavePeriodMinutesProperty, value); }
        }

        public static readonly DependencyProperty MaxNumOfSaveBackupsProperty = DependencyProperty.Register(nameof(MaxNumOfSaveBackups), typeof(int), typeof(ServerProfile), new PropertyMetadata(20));
        [DataMember]
        public int MaxNumOfSaveBackups
        {
            get { return (int)GetValue(MaxNumOfSaveBackupsProperty); }
            set { SetValue(MaxNumOfSaveBackupsProperty, value); }
        }

        public static readonly DependencyProperty MOTDProperty = DependencyProperty.Register(nameof(MOTD), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_MessageOfTheDay, ServerProfileCategory.Administration, "Message", ClearSection = true, Multiline = true, QuotedString = QuotedStringType.Remove)]
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

        public static readonly DependencyProperty MOTDDurationProperty = DependencyProperty.Register(nameof(MOTDDuration), typeof(int), typeof(ServerProfile), new PropertyMetadata(20));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_MessageOfTheDay, ServerProfileCategory.Administration, "Duration")]
        public int MOTDDuration
        {
            get { return (int)GetValue(MOTDDurationProperty); }
            set { SetValue(MOTDDurationProperty, value); }
        }

        public static readonly DependencyProperty MOTDIntervalProperty = DependencyProperty.Register(nameof(MOTDInterval), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 60)));
        [DataMember]
        public NullableValue<int> MOTDInterval
        {
            get { return (NullableValue<int>) GetValue(MOTDIntervalProperty); }
            set { SetValue(MOTDIntervalProperty, value); }
        }

        public static readonly DependencyProperty DisableValveAntiCheatSystemProperty = DependencyProperty.Register(nameof(DisableValveAntiCheatSystem), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool DisableValveAntiCheatSystem
        {
            get { return (bool)GetValue(DisableValveAntiCheatSystemProperty); }
            set { SetValue(DisableValveAntiCheatSystemProperty, value); }
        }

        public static readonly DependencyProperty DisablePlayerMovePhysicsOptimizationProperty = DependencyProperty.Register(nameof(DisablePlayerMovePhysicsOptimization), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool DisablePlayerMovePhysicsOptimization
        {
            get { return (bool)GetValue(DisablePlayerMovePhysicsOptimizationProperty); }
            set { SetValue(DisablePlayerMovePhysicsOptimizationProperty, value); }
        }

        public static readonly DependencyProperty DisableAntiSpeedHackDetectionProperty = DependencyProperty.Register(nameof(DisableAntiSpeedHackDetection), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool DisableAntiSpeedHackDetection
        {
            get { return (bool)GetValue(DisableAntiSpeedHackDetectionProperty); }
            set { SetValue(DisableAntiSpeedHackDetectionProperty, value); }
        }

        public static readonly DependencyProperty SpeedHackBiasProperty = DependencyProperty.Register(nameof(SpeedHackBias), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [DataMember]
        public float SpeedHackBias
        {
            get { return (float)GetValue(SpeedHackBiasProperty); }
            set { SetValue(SpeedHackBiasProperty, value); }
        }

        public static readonly DependencyProperty UseBattlEyeProperty = DependencyProperty.Register(nameof(UseBattlEye), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UseBattlEye
        {
            get { return (bool)GetValue(UseBattlEyeProperty); }
            set { SetValue(UseBattlEyeProperty, value); }
        }

        public static readonly DependencyProperty ForceRespawnDinosProperty = DependencyProperty.Register(nameof(ForceRespawnDinos), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool ForceRespawnDinos
        {
            get { return (bool)GetValue(ForceRespawnDinosProperty); }
            set { SetValue(ForceRespawnDinosProperty, value); }
        }

        public static readonly DependencyProperty EnableServerAutoForceRespawnWildDinosIntervalProperty = DependencyProperty.Register(nameof(EnableServerAutoForceRespawnWildDinosInterval), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableServerAutoForceRespawnWildDinosInterval
        {
            get { return (bool)GetValue(EnableServerAutoForceRespawnWildDinosIntervalProperty); }
            set { SetValue(EnableServerAutoForceRespawnWildDinosIntervalProperty, value); }
        }

        public static readonly DependencyProperty ServerAutoForceRespawnWildDinosIntervalProperty = DependencyProperty.Register(nameof(ServerAutoForceRespawnWildDinosInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(86400));
        [DataMember]
        public int ServerAutoForceRespawnWildDinosInterval
        {
            get { return (int)GetValue(ServerAutoForceRespawnWildDinosIntervalProperty); }
            set { SetValue(ServerAutoForceRespawnWildDinosIntervalProperty, value); }
        }

        public static readonly DependencyProperty EnableServerAdminLogsProperty = DependencyProperty.Register(nameof(EnableServerAdminLogs), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableServerAdminLogs
        {
            get { return (bool)GetValue(EnableServerAdminLogsProperty); }
            set { SetValue(EnableServerAdminLogsProperty, value); }
        }

        public static readonly DependencyProperty ServerAdminLogsIncludeTribeLogsProperty = DependencyProperty.Register(nameof(ServerAdminLogsIncludeTribeLogs), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool ServerAdminLogsIncludeTribeLogs
        {
            get { return (bool)GetValue(ServerAdminLogsIncludeTribeLogsProperty); }
            set { SetValue(ServerAdminLogsIncludeTribeLogsProperty, value); }
        }

        public static readonly DependencyProperty ServerRCONOutputTribeLogsProperty = DependencyProperty.Register(nameof(ServerRCONOutputTribeLogs), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool ServerRCONOutputTribeLogs
        {
            get { return (bool)GetValue(ServerRCONOutputTribeLogsProperty); }
            set { SetValue(ServerRCONOutputTribeLogsProperty, value); }
        }

        public static readonly DependencyProperty NotifyAdminCommandsInChatProperty = DependencyProperty.Register(nameof(NotifyAdminCommandsInChat), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool NotifyAdminCommandsInChat
        {
            get { return (bool)GetValue(NotifyAdminCommandsInChatProperty); }
            set { SetValue(NotifyAdminCommandsInChatProperty, value); }
        }

        public static readonly DependencyProperty MaxTribeLogsProperty = DependencyProperty.Register(nameof(MaxTribeLogs), typeof(int), typeof(ServerProfile), new PropertyMetadata(400));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Administration)]
        public int MaxTribeLogs
        {
            get { return (int)GetValue(MaxTribeLogsProperty); }
            set { SetValue(MaxTribeLogsProperty, value); }
        }

        public static readonly DependencyProperty TribeLogDestroyedEnemyStructuresProperty = DependencyProperty.Register(nameof(TribeLogDestroyedEnemyStructures), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public bool TribeLogDestroyedEnemyStructures
        {
            get { return (bool)GetValue(TribeLogDestroyedEnemyStructuresProperty); }
            set { SetValue(TribeLogDestroyedEnemyStructuresProperty, value); }
        }

        public static readonly DependencyProperty ForceDirectX10Property = DependencyProperty.Register(nameof(ForceDirectX10), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool ForceDirectX10
        {
            get { return (bool)GetValue(ForceDirectX10Property); }
            set { SetValue(ForceDirectX10Property, value); }
        }

        public static readonly DependencyProperty ForceShaderModel4Property = DependencyProperty.Register(nameof(ForceShaderModel4), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool ForceShaderModel4
        {
            get { return (bool)GetValue(ForceShaderModel4Property); }
            set { SetValue(ForceShaderModel4Property, value); }
        }

        public static readonly DependencyProperty ForceLowMemoryProperty = DependencyProperty.Register(nameof(ForceLowMemory), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool ForceLowMemory
        {
            get { return (bool)GetValue(ForceLowMemoryProperty); }
            set { SetValue(ForceLowMemoryProperty, value); }
        }

        public static readonly DependencyProperty ForceNoManSkyProperty = DependencyProperty.Register(nameof(ForceNoManSky), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool ForceNoManSky
        {
            get { return (bool)GetValue(ForceNoManSkyProperty); }
            set { SetValue(ForceNoManSkyProperty, value); }
        }

        public static readonly DependencyProperty UseNoMemoryBiasProperty = DependencyProperty.Register(nameof(UseNoMemoryBias), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UseNoMemoryBias
        {
            get { return (bool)GetValue(UseNoMemoryBiasProperty); }
            set { SetValue(UseNoMemoryBiasProperty, value); }
        }

        public static readonly DependencyProperty StasisKeepControllersProperty = DependencyProperty.Register(nameof(StasisKeepControllers), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool StasisKeepControllers
        {
            get { return (bool)GetValue(StasisKeepControllersProperty); }
            set { SetValue(StasisKeepControllersProperty, value); }
        }

        public static readonly DependencyProperty UseNoHangDetectionProperty = DependencyProperty.Register(nameof(UseNoHangDetection), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UseNoHangDetection
        {
            get { return (bool)GetValue(UseNoHangDetectionProperty); }
            set { SetValue(UseNoHangDetectionProperty, value); }
        }

        public static readonly DependencyProperty AllowHideDamageSourceFromLogsProperty = DependencyProperty.Register(nameof(AllowHideDamageSourceFromLogs), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public bool AllowHideDamageSourceFromLogs
        {
            get { return (bool)GetValue(AllowHideDamageSourceFromLogsProperty); }
            set { SetValue(AllowHideDamageSourceFromLogsProperty, value); }
        }

        public static readonly DependencyProperty ServerAllowAnselProperty = DependencyProperty.Register(nameof(ServerAllowAnsel), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool ServerAllowAnsel
        {
            get { return (bool)GetValue(ServerAllowAnselProperty); }
            set { SetValue(ServerAllowAnselProperty, value); }
        }

        public static readonly DependencyProperty StructureMemoryOptimizationsProperty = DependencyProperty.Register(nameof(StructureMemoryOptimizations), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool StructureMemoryOptimizations
        {
            get { return (bool)GetValue(StructureMemoryOptimizationsProperty); }
            set { SetValue(StructureMemoryOptimizationsProperty, value); }
        }

        public static readonly DependencyProperty UseStructureStasisGridProperty = DependencyProperty.Register(nameof(UseStructureStasisGrid), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UseStructureStasisGrid
        {
            get { return (bool)GetValue(UseStructureStasisGridProperty); }
            set { SetValue(UseStructureStasisGridProperty, value); }
        }

        public static readonly DependencyProperty NoUnderMeshCheckingProperty = DependencyProperty.Register(nameof(NoUnderMeshChecking), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool NoUnderMeshChecking
        {
            get { return (bool)GetValue(NoUnderMeshCheckingProperty); }
            set { SetValue(NoUnderMeshCheckingProperty, value); }
        }

        public static readonly DependencyProperty NoUnderMeshKillingProperty = DependencyProperty.Register(nameof(NoUnderMeshKilling), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool NoUnderMeshKilling
        {
            get { return (bool)GetValue(NoUnderMeshKillingProperty); }
            set { SetValue(NoUnderMeshKillingProperty, value); }
        }

        public static readonly DependencyProperty NoDinosProperty = DependencyProperty.Register(nameof(NoDinos), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool NoDinos
        {
            get { return (bool)GetValue(NoDinosProperty); }
            set { SetValue(NoDinosProperty, value); }
        }

        public static readonly DependencyProperty CrossplayProperty = DependencyProperty.Register(nameof(Crossplay), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool Crossplay
        {
            get { return (bool)GetValue(CrossplayProperty); }
            set { SetValue(CrossplayProperty, value); }
        }

        public static readonly DependencyProperty EpicOnlyProperty = DependencyProperty.Register(nameof(EpicOnly), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EpicOnly
        {
            get { return (bool)GetValue(EpicOnlyProperty); }
            set { SetValue(EpicOnlyProperty, value); }
        }

        public static readonly DependencyProperty EnablePublicIPForEpicProperty = DependencyProperty.Register(nameof(EnablePublicIPForEpic), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnablePublicIPForEpic
        {
            get { return (bool)GetValue(EnablePublicIPForEpicProperty); }
            set { SetValue(EnablePublicIPForEpicProperty, value); }
        }

        public static readonly DependencyProperty UseVivoxProperty = DependencyProperty.Register(nameof(UseVivox), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UseVivox
        {
            get { return (bool)GetValue(UseVivoxProperty); }
            set { SetValue(UseVivoxProperty, value); }
        }

        public static readonly DependencyProperty EnableBadWordListURLProperty = DependencyProperty.Register(nameof(EnableBadWordListURL), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableBadWordListURL
        {
            get { return (bool)GetValue(EnableBadWordListURLProperty); }
            set { SetValue(EnableBadWordListURLProperty, value); }
        }

        public static readonly DependencyProperty BadWordListURLProperty = DependencyProperty.Register(nameof(BadWordListURL), typeof(string), typeof(ServerProfile), new PropertyMetadata(""));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, ConditionedOn = nameof(EnableBadWordListURL), QuotedString = QuotedStringType.True)]
        public string BadWordListURL
        {
            get { return (string)GetValue(BadWordListURLProperty); }
            set { SetValue(BadWordListURLProperty, value); }
        }

        public static readonly DependencyProperty EnableBadWordWhiteListURLProperty = DependencyProperty.Register(nameof(EnableBadWordWhiteListURL), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableBadWordWhiteListURL
        {
            get { return (bool)GetValue(EnableBadWordWhiteListURLProperty); }
            set { SetValue(EnableBadWordWhiteListURLProperty, value); }
        }

        public static readonly DependencyProperty BadWordWhiteListURLProperty = DependencyProperty.Register(nameof(BadWordWhiteListURL), typeof(string), typeof(ServerProfile), new PropertyMetadata(""));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, ConditionedOn = nameof(EnableBadWordWhiteListURL), QuotedString = QuotedStringType.True)]
        public string BadWordWhiteListURL
        {
            get { return (string)GetValue(BadWordWhiteListURLProperty); }
            set { SetValue(BadWordWhiteListURLProperty, value); }
        }

        public static readonly DependencyProperty FilterTribeNamesProperty = DependencyProperty.Register(nameof(FilterTribeNames), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, "bFilterTribeNames")]
        public bool FilterTribeNames
        {
            get { return (bool)GetValue(FilterTribeNamesProperty); }
            set { SetValue(FilterTribeNamesProperty, value); }
        }

        public static readonly DependencyProperty FilterCharacterNamesProperty = DependencyProperty.Register(nameof(FilterCharacterNames), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, "bFilterCharacterNames")]
        public bool FilterCharacterNames
        {
            get { return (bool)GetValue(FilterCharacterNamesProperty); }
            set { SetValue(FilterCharacterNamesProperty, value); }
        }

        public static readonly DependencyProperty FilterChatProperty = DependencyProperty.Register(nameof(FilterChat), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration, "bFilterChat")]
        public bool FilterChat
        {
            get { return (bool)GetValue(FilterChatProperty); }
            set { SetValue(FilterChatProperty, value); }
        }

        public static readonly DependencyProperty OutputServerLogProperty = DependencyProperty.Register(nameof(OutputServerLog), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool OutputServerLog
        {
            get { return (bool)GetValue(OutputServerLogProperty); }
            set { SetValue(OutputServerLogProperty, value); }
        }

        public static readonly DependencyProperty AllowSharedConnectionsProperty = DependencyProperty.Register(nameof(AllowSharedConnections), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Administration)]
        public bool AllowSharedConnections
        {
            get { return (bool)GetValue(AllowSharedConnectionsProperty); }
            set { SetValue(AllowSharedConnectionsProperty, value); }
        }

        public static readonly DependencyProperty AltSaveDirectoryNameProperty = DependencyProperty.Register(nameof(AltSaveDirectoryName), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string AltSaveDirectoryName
        {
            get { return (string)GetValue(AltSaveDirectoryNameProperty); }
            set { SetValue(AltSaveDirectoryNameProperty, value); }
        }

        public static readonly DependencyProperty EnableWebAlarmProperty = DependencyProperty.Register(nameof(EnableWebAlarm), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableWebAlarm
        {
            get { return (bool)GetValue(EnableWebAlarmProperty); }
            set { SetValue(EnableWebAlarmProperty, value); }
        }

        public static readonly DependencyProperty WebAlarmKeyProperty = DependencyProperty.Register(nameof(WebAlarmKey), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string WebAlarmKey
        {
            get { return (string)GetValue(WebAlarmKeyProperty); }
            set { SetValue(WebAlarmKeyProperty, value); }
        }

        public static readonly DependencyProperty WebAlarmUrlProperty = DependencyProperty.Register(nameof(WebAlarmUrl), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string WebAlarmUrl
        {
            get { return (string)GetValue(WebAlarmUrlProperty); }
            set { SetValue(WebAlarmUrlProperty, value); }
        }

        public static readonly DependencyProperty CrossArkClusterIdProperty = DependencyProperty.Register(nameof(CrossArkClusterId), typeof(string), typeof(ServerProfile), new PropertyMetadata(string.Empty));
        [DataMember]
        public string CrossArkClusterId
        {
            get { return (string)GetValue(CrossArkClusterIdProperty); }
            set { SetValue(CrossArkClusterIdProperty, value); }
        }

        public static readonly DependencyProperty ClusterDirOverrideProperty = DependencyProperty.Register(nameof(ClusterDirOverride), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool ClusterDirOverride
        {
            get { return (bool)GetValue(ClusterDirOverrideProperty); }
            set { SetValue(ClusterDirOverrideProperty, value); }
        }

        public static readonly DependencyProperty SecureSendArKPayloadProperty = DependencyProperty.Register(nameof(SecureSendArKPayload), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool SecureSendArKPayload
        {
            get { return (bool)GetValue(SecureSendArKPayloadProperty); }
            set { SetValue(SecureSendArKPayloadProperty, value); }
        }

        public static readonly DependencyProperty UseItemDupeCheckProperty = DependencyProperty.Register(nameof(UseItemDupeCheck), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UseItemDupeCheck
        {
            get { return (bool)GetValue(UseItemDupeCheckProperty); }
            set { SetValue(UseItemDupeCheckProperty, value); }
        }

        public static readonly DependencyProperty UseSecureSpawnRulesProperty = DependencyProperty.Register(nameof(UseSecureSpawnRules), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UseSecureSpawnRules
        {
            get { return (bool)GetValue(UseSecureSpawnRulesProperty); }
            set { SetValue(UseSecureSpawnRulesProperty, value); }
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

        public static readonly DependencyProperty NewSaveFormatProperty = DependencyProperty.Register(nameof(NewSaveFormat), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool NewSaveFormat
        {
            get { return (bool)GetValue(NewSaveFormatProperty); }
            set { SetValue(NewSaveFormatProperty, value); }
        }

        public static readonly DependencyProperty UseStoreProperty = DependencyProperty.Register(nameof(UseStore), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool UseStore
        {
            get { return (bool)GetValue(UseStoreProperty); }
            set { SetValue(UseStoreProperty, value); }
        }

        public static readonly DependencyProperty BackupTransferPlayerDatasProperty = DependencyProperty.Register(nameof(BackupTransferPlayerDatas), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool BackupTransferPlayerDatas
        {
            get { return (bool)GetValue(BackupTransferPlayerDatasProperty); }
            set { SetValue(BackupTransferPlayerDatasProperty, value); }
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

        public static readonly DependencyProperty AllowDiscordClusterAliasProperty = DependencyProperty.Register(nameof(AllowDiscordClusterAlias), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool AllowDiscordClusterAlias
        {
            get { return (bool)GetValue(AllowDiscordClusterAliasProperty); }
            set { SetValue(AllowDiscordClusterAliasProperty, value); }
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
        public static readonly DependencyProperty CultureProperty = DependencyProperty.Register(nameof(Culture), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string Culture
        {
            get { return (string)GetValue(CultureProperty); }
            set { SetValue(CultureProperty, value); }
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

        public static readonly DependencyProperty EventNameProperty = DependencyProperty.Register(nameof(EventName), typeof(string), typeof(ServerProfile), new PropertyMetadata(String.Empty));
        [DataMember]
        public string EventName
        {
            get { return (string)GetValue(EventNameProperty); }
            set { SetValue(EventNameProperty, value); }
        }

        public static readonly DependencyProperty EventColorsChanceOverrideProperty = DependencyProperty.Register(nameof(EventColorsChanceOverride), typeof(float), typeof(ServerProfile), new PropertyMetadata(0.0f));
        [DataMember]
        public float EventColorsChanceOverride
        {
            get { return (float)GetValue(EventColorsChanceOverrideProperty); }
            set { SetValue(EventColorsChanceOverrideProperty, value); }
        }

        public static readonly DependencyProperty NewYear1UTCProperty = DependencyProperty.Register(nameof(NewYear1UTC), typeof(NullableValue<DateTime>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<DateTime>()));
        [DataMember]
        public NullableValue<DateTime> NewYear1UTC
        {
            get { return (NullableValue<DateTime>)GetValue(NewYear1UTCProperty); }
            set { SetValue(NewYear1UTCProperty, value); }
        }

        public static readonly DependencyProperty NewYear2UTCProperty = DependencyProperty.Register(nameof(NewYear2UTC), typeof(NullableValue<DateTime>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<DateTime>()));
        [DataMember]
        public NullableValue<DateTime> NewYear2UTC
        {
            get { return (NullableValue<DateTime>)GetValue(NewYear2UTCProperty); }
            set { SetValue(NewYear2UTCProperty, value); }
        }
        #endregion

        #region Rules
        public static readonly DependencyProperty EnableHardcoreProperty = DependencyProperty.Register(nameof(EnableHardcore), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, "ServerHardcore")]
        public bool EnableHardcore
        {
            get { return (bool)GetValue(EnableHardcoreProperty); }
            set { SetValue(EnableHardcoreProperty, value); }
        }

        public static readonly DependencyProperty EnablePVPProperty = DependencyProperty.Register(nameof(EnablePVP), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, "ServerPVE", InvertBoolean = true)]
        public bool EnablePVP
        {
            get { return (bool)GetValue(EnablePVPProperty); }
            set { SetValue(EnablePVPProperty, value); }
        }

        public static readonly DependencyProperty EnableCreativeModeProperty = DependencyProperty.Register(nameof(EnableCreativeMode), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bShowCreativeMode", WriteIfNotValue = false)]
        public bool EnableCreativeMode
        {
            get { return (bool)GetValue(EnableCreativeModeProperty); }
            set { SetValue(EnableCreativeModeProperty, value); }
        }

        public static readonly DependencyProperty AllowCaveBuildingPvEProperty = DependencyProperty.Register(nameof(AllowCaveBuildingPvE), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool AllowCaveBuildingPvE
        {
            get { return (bool)GetValue(AllowCaveBuildingPvEProperty); }
            set { SetValue(AllowCaveBuildingPvEProperty, value); }
        }

        public static readonly DependencyProperty DisableFriendlyFirePvPProperty = DependencyProperty.Register(nameof(DisableFriendlyFirePvP), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bDisableFriendlyFire")]
        public bool DisableFriendlyFirePvP
        {
            get { return (bool)GetValue(DisableFriendlyFirePvPProperty); }
            set { SetValue(DisableFriendlyFirePvPProperty, value); }
        }

        public static readonly DependencyProperty DisableFriendlyFirePvEProperty = DependencyProperty.Register(nameof(DisableFriendlyFirePvE), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bPvEDisableFriendlyFire")]
        public bool DisableFriendlyFirePvE
        {
            get { return (bool)GetValue(DisableFriendlyFirePvEProperty); }
            set { SetValue(DisableFriendlyFirePvEProperty, value); }
        }

        public static readonly DependencyProperty AllowCaveBuildingPvPProperty = DependencyProperty.Register(nameof(AllowCaveBuildingPvP), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, "AllowCaveBuildingPvP")]
        public bool AllowCaveBuildingPvP
        {
            get { return (bool)GetValue(AllowCaveBuildingPvPProperty); }
            set { SetValue(AllowCaveBuildingPvPProperty, value); }
        }

        public static readonly DependencyProperty DisableRailgunPVPProperty = DependencyProperty.Register(nameof(DisableRailgunPVP), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool DisableRailgunPVP
        {
            get { return (bool)GetValue(DisableRailgunPVPProperty); }
            set { SetValue(DisableRailgunPVPProperty, value); }
        }

        public static readonly DependencyProperty DisableLootCratesProperty = DependencyProperty.Register(nameof(DisableLootCrates), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bDisableLootCrates")]
        public bool DisableLootCrates
        {
            get { return (bool)GetValue(DisableLootCratesProperty); }
            set { SetValue(DisableLootCratesProperty, value); }
        }

        public static readonly DependencyProperty AllowCrateSpawnsOnTopOfStructuresProperty = DependencyProperty.Register(nameof(AllowCrateSpawnsOnTopOfStructures), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [DataMember]
        public bool AllowCrateSpawnsOnTopOfStructures
        {
            get { return (bool)GetValue(AllowCrateSpawnsOnTopOfStructuresProperty); }
            set { SetValue(AllowCrateSpawnsOnTopOfStructuresProperty, value); }
        }

        public static readonly DependencyProperty EnableExtraStructurePreventionVolumesProperty = DependencyProperty.Register(nameof(EnableExtraStructurePreventionVolumes), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool EnableExtraStructurePreventionVolumes
        {
            get { return (bool)GetValue(EnableExtraStructurePreventionVolumesProperty); }
            set { SetValue(EnableExtraStructurePreventionVolumesProperty, value); }
        }

        public static readonly DependencyProperty UseSingleplayerSettingsProperty = DependencyProperty.Register(nameof(UseSingleplayerSettings), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bUseSingleplayerSettings", ConditionedOn = nameof(UseSingleplayerSettings))]
        public bool UseSingleplayerSettings
        {
            get { return (bool)GetValue(UseSingleplayerSettingsProperty); }
            set { SetValue(UseSingleplayerSettingsProperty, value); }
        }

        public static readonly DependencyProperty EnableDifficultyOverrideProperty = DependencyProperty.Register(nameof(EnableDifficultyOverride), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool EnableDifficultyOverride
        {
            get { return (bool)GetValue(EnableDifficultyOverrideProperty); }
            set { SetValue(EnableDifficultyOverrideProperty, value); }
        }

        public static readonly DependencyProperty OverrideOfficialDifficultyProperty = DependencyProperty.Register(nameof(OverrideOfficialDifficulty), typeof(float), typeof(ServerProfile), new PropertyMetadata(4.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(EnableDifficultyOverride))]
        public float OverrideOfficialDifficulty
        {
            get { return (float)GetValue(OverrideOfficialDifficultyProperty); }
            set { SetValue(OverrideOfficialDifficultyProperty, value); }
        }

        public static readonly DependencyProperty DifficultyOffsetProperty = DependencyProperty.Register(nameof(DifficultyOffset), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(EnableDifficultyOverride))]
        public float DifficultyOffset
        {
            get { return (float)GetValue(DifficultyOffsetProperty); }
            set { SetValue(DifficultyOffsetProperty, value); }
        }

        public static readonly DependencyProperty DestroyTamesOverLevelClamp﻿Property = DependencyProperty.Register(nameof(DestroyTamesOverLevelClamp), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 0)]
        public int DestroyTamesOverLevelClamp
        {
            get { return (int)GetValue(DestroyTamesOverLevelClamp﻿Property); }
            set { SetValue(DestroyTamesOverLevelClamp﻿Property, value); }
        }


        public static readonly DependencyProperty EnableTributeDownloadsProperty = DependencyProperty.Register(nameof(EnableTributeDownloads), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, "NoTributeDownloads", InvertBoolean = true)]
        public bool EnableTributeDownloads
        {
            get { return (bool)GetValue(EnableTributeDownloadsProperty); }
            set { SetValue(EnableTributeDownloadsProperty, value); }
        }

        public static readonly DependencyProperty PreventDownloadSurvivorsProperty = DependencyProperty.Register(nameof(PreventDownloadSurvivors), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(EnableTributeDownloads))]
        public bool PreventDownloadSurvivors
        {
            get { return (bool)GetValue(PreventDownloadSurvivorsProperty); }
            set { SetValue(PreventDownloadSurvivorsProperty, value); }
        }

        public static readonly DependencyProperty PreventDownloadItemsProperty = DependencyProperty.Register(nameof(PreventDownloadItems), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(EnableTributeDownloads))]
        public bool PreventDownloadItems
        {
            get { return (bool)GetValue(PreventDownloadItemsProperty); }
            set { SetValue(PreventDownloadItemsProperty, value); }
        }

        public static readonly DependencyProperty PreventDownloadDinosProperty = DependencyProperty.Register(nameof(PreventDownloadDinos), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(EnableTributeDownloads))]
        public bool PreventDownloadDinos
        {
            get { return (bool)GetValue(PreventDownloadDinosProperty); }
            set { SetValue(PreventDownloadDinosProperty, value); }
        }

        public static readonly DependencyProperty PreventUploadSurvivorsProperty = DependencyProperty.Register(nameof(PreventUploadSurvivors), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool PreventUploadSurvivors
        {
            get { return (bool)GetValue(PreventUploadSurvivorsProperty); }
            set { SetValue(PreventUploadSurvivorsProperty, value); }
        }

        public static readonly DependencyProperty PreventUploadItemsProperty = DependencyProperty.Register(nameof(PreventUploadItems), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool PreventUploadItems
        {
            get { return (bool)GetValue(PreventUploadItemsProperty); }
            set { SetValue(PreventUploadItemsProperty, value); }
        }

        public static readonly DependencyProperty PreventUploadDinosProperty = DependencyProperty.Register(nameof(PreventUploadDinos), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool PreventUploadDinos
        {
            get { return (bool)GetValue(PreventUploadDinosProperty); }
            set { SetValue(PreventUploadDinosProperty, value); }
        }

        public static readonly DependencyProperty MaxTributeDinosProperty = DependencyProperty.Register(nameof(MaxTributeDinos), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 20)));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public NullableValue<int> MaxTributeDinos
        {
            get { return (NullableValue<int>)GetValue(MaxTributeDinosProperty); }
            set { SetValue(MaxTributeDinosProperty, value); }
        }

        public static readonly DependencyProperty MaxTributeItemsProperty = DependencyProperty.Register(nameof(MaxTributeItems), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 50)));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public NullableValue<int> MaxTributeItems
        {
            get { return (NullableValue<int>)GetValue(MaxTributeItemsProperty); }
            set { SetValue(MaxTributeItemsProperty, value); }
        }

        public static readonly DependencyProperty NoTransferFromFilteringProperty = DependencyProperty.Register(nameof(NoTransferFromFiltering), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool NoTransferFromFiltering
        {
            get { return (bool)GetValue(NoTransferFromFilteringProperty); }
            set { SetValue(NoTransferFromFilteringProperty, value); }
        }

        public static readonly DependencyProperty DisableCustomFoldersInTributeInventoriesProperty = DependencyProperty.Register(nameof(DisableCustomFoldersInTributeInventories), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool DisableCustomFoldersInTributeInventories
        {
            get { return (bool)GetValue(DisableCustomFoldersInTributeInventoriesProperty); }
            set { SetValue(DisableCustomFoldersInTributeInventoriesProperty, value); }
        }

        public static readonly DependencyProperty OverrideTributeCharacterExpirationSecondsProperty = DependencyProperty.Register(nameof(OverrideTributeCharacterExpirationSeconds), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool OverrideTributeCharacterExpirationSeconds
        {
            get { return (bool)GetValue(OverrideTributeCharacterExpirationSecondsProperty); }
            set { SetValue(OverrideTributeCharacterExpirationSecondsProperty, value); }
        }

        public static readonly DependencyProperty OverrideTributeItemExpirationSecondsProperty = DependencyProperty.Register(nameof(OverrideTributeItemExpirationSeconds), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool OverrideTributeItemExpirationSeconds
        {
            get { return (bool)GetValue(OverrideTributeItemExpirationSecondsProperty); }
            set { SetValue(OverrideTributeItemExpirationSecondsProperty, value); }
        }

        public static readonly DependencyProperty OverrideTributeDinoExpirationSecondsProperty = DependencyProperty.Register(nameof(OverrideTributeDinoExpirationSeconds), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool OverrideTributeDinoExpirationSeconds
        {
            get { return (bool)GetValue(OverrideTributeDinoExpirationSecondsProperty); }
            set { SetValue(OverrideTributeDinoExpirationSecondsProperty, value); }
        }

        public static readonly DependencyProperty OverrideMinimumDinoReuploadIntervalProperty = DependencyProperty.Register(nameof(OverrideMinimumDinoReuploadInterval), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool OverrideMinimumDinoReuploadInterval
        {
            get { return (bool)GetValue(OverrideMinimumDinoReuploadIntervalProperty); }
            set { SetValue(OverrideMinimumDinoReuploadIntervalProperty, value); }
        }

        public bool SaveTributeCharacterExpirationSeconds
        {
            get { return !string.IsNullOrWhiteSpace(this.CrossArkClusterId) && OverrideTributeCharacterExpirationSeconds; }
#pragma warning disable CS1717 // Assignment made to same variable
            set { value = value; }
#pragma warning restore CS1717 // Assignment made to same variable
        }

        public bool SaveTributeItemExpirationSeconds
        {
            get { return !string.IsNullOrWhiteSpace(this.CrossArkClusterId) && OverrideTributeItemExpirationSeconds; }
#pragma warning disable CS1717 // Assignment made to same variable
            set { value = value; }
#pragma warning restore CS1717 // Assignment made to same variable
        }

        public bool SaveTributeDinoExpirationSeconds
        {
            get { return !string.IsNullOrWhiteSpace(this.CrossArkClusterId) && OverrideTributeDinoExpirationSeconds; }
#pragma warning disable CS1717 // Assignment made to same variable
            set { value = value; }
#pragma warning restore CS1717 // Assignment made to same variable
        }

        public bool SaveMinimumDinoReuploadInterval
        {
            get { return !string.IsNullOrWhiteSpace(this.CrossArkClusterId) && OverrideMinimumDinoReuploadInterval; }
#pragma warning disable CS1717 // Assignment made to same variable
            set { value = value; }
#pragma warning restore CS1717 // Assignment made to same variable
        }

        public static readonly DependencyProperty TributeCharacterExpirationSecondsProperty = DependencyProperty.Register(nameof(TributeCharacterExpirationSeconds), typeof(int), typeof(ServerProfile), new PropertyMetadata(86400));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(SaveTributeCharacterExpirationSeconds))]
        public int TributeCharacterExpirationSeconds
        {
            get { return (int)GetValue(TributeCharacterExpirationSecondsProperty); }
            set { SetValue(TributeCharacterExpirationSecondsProperty, value); }
        }

        public static readonly DependencyProperty TributeItemExpirationSecondsProperty = DependencyProperty.Register(nameof(TributeItemExpirationSeconds), typeof(int), typeof(ServerProfile), new PropertyMetadata(86400));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(SaveTributeItemExpirationSeconds))]
        public int TributeItemExpirationSeconds
        {
            get { return (int)GetValue(TributeItemExpirationSecondsProperty); }
            set { SetValue(TributeItemExpirationSecondsProperty, value); }
        }

        public static readonly DependencyProperty TributeDinoExpirationSecondsProperty = DependencyProperty.Register(nameof(TributeDinoExpirationSeconds), typeof(int), typeof(ServerProfile), new PropertyMetadata(86400));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(SaveTributeDinoExpirationSeconds))]
        public int TributeDinoExpirationSeconds
        {
            get { return (int)GetValue(TributeDinoExpirationSecondsProperty); }
            set { SetValue(TributeDinoExpirationSecondsProperty, value); }
        }

        public static readonly DependencyProperty MinimumDinoReuploadIntervalProperty = DependencyProperty.Register(nameof(MinimumDinoReuploadInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(43200));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(SaveMinimumDinoReuploadInterval))]
        public int MinimumDinoReuploadInterval
        {
            get { return (int)GetValue(MinimumDinoReuploadIntervalProperty); }
            set { SetValue(MinimumDinoReuploadIntervalProperty, value); }
        }

        public static readonly DependencyProperty CrossARKAllowForeignDinoDownloadsProperty = DependencyProperty.Register(nameof(CrossARKAllowForeignDinoDownloads), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool CrossARKAllowForeignDinoDownloads
        {
            get { return (bool)GetValue(CrossARKAllowForeignDinoDownloadsProperty); }
            set { SetValue(CrossARKAllowForeignDinoDownloadsProperty, value); }
        }

        public static readonly DependencyProperty IncreasePvPRespawnIntervalProperty = DependencyProperty.Register(nameof(IncreasePvPRespawnInterval), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bIncreasePvPRespawnInterval")]
        public bool IncreasePvPRespawnInterval
        {
            get { return (bool)GetValue(IncreasePvPRespawnIntervalProperty); }
            set { SetValue(IncreasePvPRespawnIntervalProperty, value); }
        }

        public static readonly DependencyProperty IncreasePvPRespawnIntervalCheckPeriodProperty = DependencyProperty.Register(nameof(IncreasePvPRespawnIntervalCheckPeriod), typeof(int), typeof(ServerProfile), new PropertyMetadata(300));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, ConditionedOn = nameof(IncreasePvPRespawnInterval))]
        public int IncreasePvPRespawnIntervalCheckPeriod
        {
            get { return (int)GetValue(IncreasePvPRespawnIntervalCheckPeriodProperty); }
            set { SetValue(IncreasePvPRespawnIntervalCheckPeriodProperty, value); }
        }

        public static readonly DependencyProperty IncreasePvPRespawnIntervalMultiplierProperty = DependencyProperty.Register(nameof(IncreasePvPRespawnIntervalMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, ConditionedOn = nameof(IncreasePvPRespawnInterval))]
        public float IncreasePvPRespawnIntervalMultiplier
        {
            get { return (float)GetValue(IncreasePvPRespawnIntervalMultiplierProperty); }
            set { SetValue(IncreasePvPRespawnIntervalMultiplierProperty, value); }
        }

        public static readonly DependencyProperty IncreasePvPRespawnIntervalBaseAmountProperty = DependencyProperty.Register(nameof(IncreasePvPRespawnIntervalBaseAmount), typeof(int), typeof(ServerProfile), new PropertyMetadata(60));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, ConditionedOn = nameof(IncreasePvPRespawnInterval))]
        public int IncreasePvPRespawnIntervalBaseAmount
        {
            get { return (int)GetValue(IncreasePvPRespawnIntervalBaseAmountProperty); }
            set { SetValue(IncreasePvPRespawnIntervalBaseAmountProperty, value); }
        }

        public static readonly DependencyProperty PreventOfflinePvPProperty = DependencyProperty.Register(nameof(PreventOfflinePvP), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool PreventOfflinePvP
        {
            get { return (bool)GetValue(PreventOfflinePvPProperty); }
            set { SetValue(PreventOfflinePvPProperty, value); }
        }

        public static readonly DependencyProperty PreventOfflinePvPIntervalProperty = DependencyProperty.Register(nameof(PreventOfflinePvPInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(900));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(PreventOfflinePvP))]
        public int PreventOfflinePvPInterval
        {
            get { return (int)GetValue(PreventOfflinePvPIntervalProperty); }
            set { SetValue(PreventOfflinePvPIntervalProperty, value); }
        }

        public static readonly DependencyProperty PreventOfflinePvPConnectionInvincibleIntervalProperty = DependencyProperty.Register(nameof(PreventOfflinePvPConnectionInvincibleInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(5));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, ConditionedOn = nameof(PreventOfflinePvP))]
        public int PreventOfflinePvPConnectionInvincibleInterval
        {
            get { return (int)GetValue(PreventOfflinePvPConnectionInvincibleIntervalProperty); }
            set { SetValue(PreventOfflinePvPConnectionInvincibleIntervalProperty, value); }
        }

        public static readonly DependencyProperty AutoPvETimerProperty = DependencyProperty.Register(nameof(AutoPvETimer), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bAutoPvETimer")]
        public bool AutoPvETimer
        {
            get { return (bool)GetValue(AutoPvETimerProperty); }
            set { SetValue(AutoPvETimerProperty, value); }
        }

        public static readonly DependencyProperty AutoPvEUseSystemTimeProperty = DependencyProperty.Register(nameof(AutoPvEUseSystemTime), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bAutoPvEUseSystemTime", ConditionedOn = nameof(AutoPvETimer))]
        public bool AutoPvEUseSystemTime
        {
            get { return (bool)GetValue(AutoPvEUseSystemTimeProperty); }
            set { SetValue(AutoPvEUseSystemTimeProperty, value); }
        }

        public static readonly DependencyProperty AutoPvEStartTimeSecondsProperty = DependencyProperty.Register(nameof(AutoPvEStartTimeSeconds), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, ConditionedOn = nameof(AutoPvETimer))]
        public int AutoPvEStartTimeSeconds
        {
            get { return (int)GetValue(AutoPvEStartTimeSecondsProperty); }
            set { SetValue(AutoPvEStartTimeSecondsProperty, value); }
        }

        public static readonly DependencyProperty AutoPvEStopTimeSecondsProperty = DependencyProperty.Register(nameof(AutoPvEStopTimeSeconds), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, ConditionedOn = nameof(AutoPvETimer))]
        public int AutoPvEStopTimeSeconds
        {
            get { return (int)GetValue(AutoPvEStopTimeSecondsProperty); }
            set { SetValue(AutoPvEStopTimeSecondsProperty, value); }
        }

        public static readonly DependencyProperty MaxNumberOfPlayersInTribeProperty = DependencyProperty.Register(nameof(MaxNumberOfPlayersInTribe), typeof(int), typeof(ServerProfile), new PropertyMetadata(70));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules)]
        public int MaxNumberOfPlayersInTribe
        {
            get { return (int)GetValue(MaxNumberOfPlayersInTribeProperty); }
            set { SetValue(MaxNumberOfPlayersInTribeProperty, value); }
        }

        public static readonly DependencyProperty TribeNameChangeCooldownProperty = DependencyProperty.Register(nameof(TribeNameChangeCooldown), typeof(int), typeof(ServerProfile), new PropertyMetadata(15));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public int TribeNameChangeCooldown
        {
            get { return (int)GetValue(TribeNameChangeCooldownProperty); }
            set { SetValue(TribeNameChangeCooldownProperty, value); }
        }

        public static readonly DependencyProperty TribeSlotReuseCooldownProperty = DependencyProperty.Register(nameof(TribeSlotReuseCooldown), typeof(float), typeof(ServerProfile), new PropertyMetadata(0.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 0.0f)]
        public float TribeSlotReuseCooldown
        {
            get { return (float)GetValue(TribeSlotReuseCooldownProperty); }
            set { SetValue(TribeSlotReuseCooldownProperty, value); }
        }

        public static readonly DependencyProperty AllowTribeAlliancesProperty = DependencyProperty.Register(nameof(AllowTribeAlliances), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, "PreventTribeAlliances", InvertBoolean = true)]
        public bool AllowTribeAlliances
        {
            get { return (bool)GetValue(AllowTribeAlliancesProperty); }
            set { SetValue(AllowTribeAlliancesProperty, value); }
        }

        public static readonly DependencyProperty MaxAlliancesPerTribeProperty = DependencyProperty.Register(nameof(MaxAlliancesPerTribe), typeof(int), typeof(ServerProfile), new PropertyMetadata(10));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, ConditionedOn = nameof(AllowTribeAlliances))]
        public int MaxAlliancesPerTribe
        {
            get { return (int)GetValue(MaxAlliancesPerTribeProperty); }
            set { SetValue(MaxAlliancesPerTribeProperty, value); }
        }

        public static readonly DependencyProperty MaxTribesPerAllianceProperty = DependencyProperty.Register(nameof(MaxTribesPerAlliance), typeof(int), typeof(ServerProfile), new PropertyMetadata(10));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, ConditionedOn = nameof(AllowTribeAlliances))]
        public int MaxTribesPerAlliance
        {
            get { return (int)GetValue(MaxTribesPerAllianceProperty); }
            set { SetValue(MaxTribesPerAllianceProperty, value); }
        }

        public static readonly DependencyProperty AllowTribeWarPvEProperty = DependencyProperty.Register(nameof(AllowTribeWarPvE), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bPvEAllowTribeWar")]
        public bool AllowTribeWarPvE
        {
            get { return (bool)GetValue(AllowTribeWarPvEProperty); }
            set { SetValue(AllowTribeWarPvEProperty, value); }
        }

        public static readonly DependencyProperty AllowTribeWarCancelPvEProperty = DependencyProperty.Register(nameof(AllowTribeWarCancelPvE), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bPvEAllowTribeWarCancel")]
        public bool AllowTribeWarCancelPvE
        {
            get { return (bool)GetValue(AllowTribeWarCancelPvEProperty); }
            set { SetValue(AllowTribeWarCancelPvEProperty, value); }
        }

        public static readonly DependencyProperty AllowCustomRecipesProperty = DependencyProperty.Register(nameof(AllowCustomRecipes), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bAllowCustomRecipes")]
        public bool AllowCustomRecipes
        {
            get { return (bool)GetValue(AllowCustomRecipesProperty); }
            set { SetValue(AllowCustomRecipesProperty, value); }
        }

        public static readonly DependencyProperty CustomRecipeEffectivenessMultiplierProperty = DependencyProperty.Register(nameof(CustomRecipeEffectivenessMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 1.0f)]
        public float CustomRecipeEffectivenessMultiplier
        {
            get { return (float)GetValue(CustomRecipeEffectivenessMultiplierProperty); }
            set { SetValue(CustomRecipeEffectivenessMultiplierProperty, value); }
        }

        public static readonly DependencyProperty CustomRecipeSkillMultiplierProperty = DependencyProperty.Register(nameof(CustomRecipeSkillMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 1.0f)]
        public float CustomRecipeSkillMultiplier
        {
            get { return (float)GetValue(CustomRecipeSkillMultiplierProperty); }
            set { SetValue(CustomRecipeSkillMultiplierProperty, value); }
        }

        public static readonly DependencyProperty EnableDiseasesProperty = DependencyProperty.Register(nameof(EnableDiseases), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, "PreventDiseases", InvertBoolean = true)]
        public bool EnableDiseases
        {
            get { return (bool)GetValue(EnableDiseasesProperty); }
            set { SetValue(EnableDiseasesProperty, value); }
        }

        public static readonly DependencyProperty NonPermanentDiseasesProperty = DependencyProperty.Register(nameof(NonPermanentDiseases), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(EnableDiseases))]
        public bool NonPermanentDiseases
        {
            get { return (bool)GetValue(NonPermanentDiseasesProperty); }
            set { SetValue(NonPermanentDiseasesProperty, value); }
        }

        public static readonly DependencyProperty OverrideNPCNetworkStasisRangeScaleProperty = DependencyProperty.Register(nameof(OverrideNPCNetworkStasisRangeScale), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool OverrideNPCNetworkStasisRangeScale
        {
            get { return (bool)GetValue(OverrideNPCNetworkStasisRangeScaleProperty); }
            set { SetValue(OverrideNPCNetworkStasisRangeScaleProperty, value); }
        }

        public static readonly DependencyProperty NPCNetworkStasisRangeScalePlayerCountStartProperty = DependencyProperty.Register(nameof(NPCNetworkStasisRangeScalePlayerCountStart), typeof(int), typeof(ServerProfile), new PropertyMetadata(70));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(OverrideNPCNetworkStasisRangeScale))]
        public int NPCNetworkStasisRangeScalePlayerCountStart
        {
            get { return (int)GetValue(NPCNetworkStasisRangeScalePlayerCountStartProperty); }
            set { SetValue(NPCNetworkStasisRangeScalePlayerCountStartProperty, value); }
        }

        public static readonly DependencyProperty NPCNetworkStasisRangeScalePlayerCountEndProperty = DependencyProperty.Register(nameof(NPCNetworkStasisRangeScalePlayerCountEnd), typeof(int), typeof(ServerProfile), new PropertyMetadata(120));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(OverrideNPCNetworkStasisRangeScale))]
        public int NPCNetworkStasisRangeScalePlayerCountEnd
        {
            get { return (int)GetValue(NPCNetworkStasisRangeScalePlayerCountEndProperty); }
            set { SetValue(NPCNetworkStasisRangeScalePlayerCountEndProperty, value); }
        }

        public static readonly DependencyProperty NPCNetworkStasisRangeScalePercentEndProperty = DependencyProperty.Register(nameof(NPCNetworkStasisRangeScalePercentEnd), typeof(float), typeof(ServerProfile), new PropertyMetadata(0.5f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(OverrideNPCNetworkStasisRangeScale))]
        public float NPCNetworkStasisRangeScalePercentEnd
        {
            get { return (float)GetValue(NPCNetworkStasisRangeScalePercentEndProperty); }
            set { SetValue(NPCNetworkStasisRangeScalePercentEndProperty, value); }
        }

        public static readonly DependencyProperty UseCorpseLocatorProperty = DependencyProperty.Register(nameof(UseCorpseLocator), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bUseCorpseLocator")]
        public bool UseCorpseLocator
        {
            get { return (bool)GetValue(UseCorpseLocatorProperty); }
            set { SetValue(UseCorpseLocatorProperty, value); }
        }

        public static readonly DependencyProperty MinimumTimeBetweenInventoryRetrievalProperty = DependencyProperty.Register(nameof(MinimumTimeBetweenInventoryRetrieval), typeof(int), typeof(ServerProfile), new PropertyMetadata(3600));
        [DataMember]
        public int MinimumTimeBetweenInventoryRetrieval
        {
            get { return (int)GetValue(MinimumTimeBetweenInventoryRetrievalProperty); }
            set { SetValue(MinimumTimeBetweenInventoryRetrievalProperty, value); }
        }

        public static readonly DependencyProperty PreventSpawnAnimationsProperty = DependencyProperty.Register(nameof(PreventSpawnAnimations), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool PreventSpawnAnimations
        {
            get { return (bool)GetValue(PreventSpawnAnimationsProperty); }
            set { SetValue(PreventSpawnAnimationsProperty, value); }
        }

        public static readonly DependencyProperty AllowUnlimitedRespecsProperty = DependencyProperty.Register(nameof(AllowUnlimitedRespecs), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bAllowUnlimitedRespecs")]
        public bool AllowUnlimitedRespecs
        {
            get { return (bool)GetValue(AllowUnlimitedRespecsProperty); }
            set { SetValue(AllowUnlimitedRespecsProperty, value); }
        }

        public static readonly DependencyProperty AllowPlatformSaddleMultiFloorsProperty = DependencyProperty.Register(nameof(AllowPlatformSaddleMultiFloors), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bAllowPlatformSaddleMultiFloors")]
        public bool AllowPlatformSaddleMultiFloors
        {
            get { return (bool)GetValue(AllowPlatformSaddleMultiFloorsProperty); }
            set { SetValue(AllowPlatformSaddleMultiFloorsProperty, value); }
        }

        public static readonly DependencyProperty PlatformSaddleBuildAreaBoundsMultiplierProperty = DependencyProperty.Register(nameof(PlatformSaddleBuildAreaBoundsMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public float PlatformSaddleBuildAreaBoundsMultiplier
        {
            get { return (float)GetValue(PlatformSaddleBuildAreaBoundsMultiplierProperty); }
            set { SetValue(PlatformSaddleBuildAreaBoundsMultiplierProperty, value); }
        }

        public static readonly DependencyProperty MaxGateFrameOnSaddlesProperty = DependencyProperty.Register(nameof(MaxGateFrameOnSaddles), typeof(int), typeof(ServerProfile), new PropertyMetadata(2));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public int MaxGateFrameOnSaddles
        {
            get { return (int)GetValue(MaxGateFrameOnSaddlesProperty); }
            set { SetValue(MaxGateFrameOnSaddlesProperty, value); }
        }

        public static readonly DependencyProperty OxygenSwimSpeedStatMultiplierProperty = DependencyProperty.Register(nameof(OxygenSwimSpeedStatMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, WriteIfNotValue = 1.0f)]
        public float OxygenSwimSpeedStatMultiplier
        {
            get { return (float)GetValue(OxygenSwimSpeedStatMultiplierProperty); }
            set { SetValue(OxygenSwimSpeedStatMultiplierProperty, value); }
        }

        public static readonly DependencyProperty SupplyCrateLootQualityMultiplierProperty = DependencyProperty.Register(nameof(SupplyCrateLootQualityMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 1.0f)]
        public float SupplyCrateLootQualityMultiplier
        {
            get { return (float)GetValue(SupplyCrateLootQualityMultiplierProperty); }
            set { SetValue(SupplyCrateLootQualityMultiplierProperty, value); }
        }

        public static readonly DependencyProperty FishingLootQualityMultiplierProperty = DependencyProperty.Register(nameof(FishingLootQualityMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 1.0f)]
        public float FishingLootQualityMultiplier
        {
            get { return (float)GetValue(FishingLootQualityMultiplierProperty); }
            set { SetValue(FishingLootQualityMultiplierProperty, value); }
        }

        public static readonly DependencyProperty UseCorpseLifeSpanMultiplierProperty = DependencyProperty.Register(nameof(UseCorpseLifeSpanMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 1.0f)]
        public float UseCorpseLifeSpanMultiplier
        {
            get { return (float)GetValue(UseCorpseLifeSpanMultiplierProperty); }
            set { SetValue(UseCorpseLifeSpanMultiplierProperty, value); }
        }

        public static readonly DependencyProperty GlobalPoweredBatteryDurabilityDecreasePerSecondProperty = DependencyProperty.Register(nameof(GlobalPoweredBatteryDurabilityDecreasePerSecond), typeof(float), typeof(ServerProfile), new PropertyMetadata(3.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 4.0f)]
        public float GlobalPoweredBatteryDurabilityDecreasePerSecond
        {
            get { return (float)GetValue(GlobalPoweredBatteryDurabilityDecreasePerSecondProperty); }
            set { SetValue(GlobalPoweredBatteryDurabilityDecreasePerSecondProperty, value); }
        }

        public static readonly DependencyProperty RandomSupplyCratePointsProperty = DependencyProperty.Register(nameof(RandomSupplyCratePoints), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool RandomSupplyCratePoints
        {
            get { return (bool)GetValue(RandomSupplyCratePointsProperty); }
            set { SetValue(RandomSupplyCratePointsProperty, value); }
        }

        public static readonly DependencyProperty FuelConsumptionIntervalMultiplierProperty = DependencyProperty.Register(nameof(FuelConsumptionIntervalMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 1.0f)]
        public float FuelConsumptionIntervalMultiplier
        {
            get { return (float)GetValue(FuelConsumptionIntervalMultiplierProperty); }
            set { SetValue(FuelConsumptionIntervalMultiplierProperty, value); }
        }

        public static readonly DependencyProperty LimitNonPlayerDroppedItemsRangeProperty = DependencyProperty.Register(nameof(LimitNonPlayerDroppedItemsRange), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 0)]
        public int LimitNonPlayerDroppedItemsRange
        {
            get { return (int)GetValue(LimitNonPlayerDroppedItemsRangeProperty); }
            set { SetValue(LimitNonPlayerDroppedItemsRangeProperty, value); }
        }

        public static readonly DependencyProperty LimitNonPlayerDroppedItemsCountProperty = DependencyProperty.Register(nameof(LimitNonPlayerDroppedItemsCount), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, WriteIfNotValue = 0)]
        public int LimitNonPlayerDroppedItemsCount
        {
            get { return (int)GetValue(LimitNonPlayerDroppedItemsCountProperty); }
            set { SetValue(LimitNonPlayerDroppedItemsCountProperty, value); }
        }

        public static readonly DependencyProperty EnableCryopodNerfProperty = DependencyProperty.Register(nameof(EnableCryopodNerf), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(EnableCryopodNerf))]
        public bool EnableCryopodNerf
        {
            get { return (bool)GetValue(EnableCryopodNerfProperty); }
            set { SetValue(EnableCryopodNerfProperty, value); }
        }

        public static readonly DependencyProperty CryopodNerfDurationProperty = DependencyProperty.Register(nameof(CryopodNerfDuration), typeof(float), typeof(ServerProfile), new PropertyMetadata(10.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(EnableCryopodNerf))]
        public float CryopodNerfDuration
        {
            get { return (float)GetValue(CryopodNerfDurationProperty); }
            set { SetValue(CryopodNerfDurationProperty, value); }
        }

        public static readonly DependencyProperty CryopodNerfDamageMultiplierProperty = DependencyProperty.Register(nameof(CryopodNerfDamageMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, "CryopodNerfDamageMult", ConditionedOn = nameof(EnableCryopodNerf))]
        public float CryopodNerfDamageMultiplier
        {
            get { return (float)GetValue(CryopodNerfDamageMultiplierProperty); }
            set { SetValue(CryopodNerfDamageMultiplierProperty, value); }
        }

        public static readonly DependencyProperty CryopodNerfIncomingDamageMultiplierPercentProperty = DependencyProperty.Register(nameof(CryopodNerfIncomingDamageMultiplierPercent), typeof(float), typeof(ServerProfile), new PropertyMetadata(0.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, "CryopodNerfIncomingDamageMultPercent", ConditionedOn = nameof(EnableCryopodNerf))]
        public float CryopodNerfIncomingDamageMultiplierPercent
        {
            get { return (float)GetValue(CryopodNerfIncomingDamageMultiplierPercentProperty); }
            set { SetValue(CryopodNerfIncomingDamageMultiplierPercentProperty, value); }
        }

        public static readonly DependencyProperty DisableGenesisMissionsProperty = DependencyProperty.Register(nameof(DisableGenesisMissions), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bDisableGenesisMissions")]
        public bool DisableGenesisMissions
        {
            get { return (bool)GetValue(DisableGenesisMissionsProperty); }
            set { SetValue(DisableGenesisMissionsProperty, value); }
        }

        public static readonly DependencyProperty AllowTekSuitPowersInGenesisProperty = DependencyProperty.Register(nameof(AllowTekSuitPowersInGenesis), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool AllowTekSuitPowersInGenesis
        {
            get { return (bool)GetValue(AllowTekSuitPowersInGenesisProperty); }
            set { SetValue(AllowTekSuitPowersInGenesisProperty, value); }
        }

        public static readonly DependencyProperty DisableDefaultMapItemSetsProperty = DependencyProperty.Register(nameof(DisableDefaultMapItemSets), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bDisableDefaultMapItemSets")]
        public bool DisableDefaultMapItemSets
        {
            get { return (bool)GetValue(DisableDefaultMapItemSetsProperty); }
            set { SetValue(DisableDefaultMapItemSetsProperty, value); }
        }

        public static readonly DependencyProperty DisableWorldBuffsProperty = DependencyProperty.Register(nameof(DisableWorldBuffs), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bDisableWorldBuffs")]
        public bool DisableWorldBuffs
        {
            get { return (bool)GetValue(DisableWorldBuffsProperty); }
            set { SetValue(DisableWorldBuffsProperty, value); }
        }

        public static readonly DependencyProperty EnableWorldBuffScalingProperty = DependencyProperty.Register(nameof(EnableWorldBuffScaling), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bEnableWorldBuffScaling")]
        public bool EnableWorldBuffScaling
        {
            get { return (bool)GetValue(EnableWorldBuffScalingProperty); }
            set { SetValue(EnableWorldBuffScalingProperty, value); }
        }

        public static readonly DependencyProperty WorldBuffScalingEfficacyProperty = DependencyProperty.Register(nameof(WorldBuffScalingEfficacy), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, ConditionedOn = nameof(EnableWorldBuffScaling))]
        public float WorldBuffScalingEfficacy
        {
            get { return (float)GetValue(WorldBuffScalingEfficacyProperty); }
            set { SetValue(WorldBuffScalingEfficacyProperty, value); }
        }

        public static readonly DependencyProperty AdjustableMutagenSpawnDelayMultiplierProperty = DependencyProperty.Register(nameof(AdjustableMutagenSpawnDelayMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules)]
        public float AdjustableMutagenSpawnDelayMultiplier
        {
            get { return (float)GetValue(AdjustableMutagenSpawnDelayMultiplierProperty); }
            set { SetValue(AdjustableMutagenSpawnDelayMultiplierProperty, value); }
        }

        public static readonly DependencyProperty EnableCryoSicknessPVEProperty = DependencyProperty.Register(nameof(EnableCryoSicknessPVE), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public bool EnableCryoSicknessPVE
        {
            get { return (bool)GetValue(EnableCryoSicknessPVEProperty); }
            set { SetValue(EnableCryoSicknessPVEProperty, value); }
        }

        public static readonly DependencyProperty MaxHexagonsPerCharacterProperty = DependencyProperty.Register(nameof(MaxHexagonsPerCharacter), typeof(int), typeof(ServerProfile), new PropertyMetadata(2000000000));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules)]
        public int MaxHexagonsPerCharacter
        {
            get { return (int)GetValue(MaxHexagonsPerCharacterProperty); }
            set { SetValue(MaxHexagonsPerCharacterProperty, value); }
        }

        public static readonly DependencyProperty DisableHexagonStoreProperty = DependencyProperty.Register(nameof(DisableHexagonStore), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bDisableHexagonStore", ConditionedOn = nameof(DisableHexagonStore))]
        public bool DisableHexagonStore
        {
            get { return (bool)GetValue(DisableHexagonStoreProperty); }
            set { SetValue(DisableHexagonStoreProperty, value); }
        }

        public static readonly DependencyProperty HexStoreAllowOnlyEngramTradeOptionProperty = DependencyProperty.Register(nameof(HexStoreAllowOnlyEngramTradeOption), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "bHexStoreAllowOnlyEngramTradeOption", ConditionedOn = nameof(HexStoreAllowOnlyEngramTradeOption))]
        public bool HexStoreAllowOnlyEngramTradeOption
        {
            get { return (bool)GetValue(HexStoreAllowOnlyEngramTradeOptionProperty); }
            set { SetValue(HexStoreAllowOnlyEngramTradeOptionProperty, value); }
        }

        public static readonly DependencyProperty HexagonRewardMultiplierProperty = DependencyProperty.Register(nameof(HexagonRewardMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "BaseHexagonRewardMultiplier")]
        public float HexagonRewardMultiplier
        {
            get { return (float)GetValue(HexagonRewardMultiplierProperty); }
            set { SetValue(HexagonRewardMultiplierProperty, value); }
        }

        public static readonly DependencyProperty HexagonCostMultiplierProperty = DependencyProperty.Register(nameof(HexagonCostMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules)]
        public float HexagonCostMultiplier
        {
            get { return (float)GetValue(HexagonCostMultiplierProperty); }
            set { SetValue(HexagonCostMultiplierProperty, value); }
        }

        public static readonly DependencyProperty Ragnarok_EnableSettingsProperty = DependencyProperty.Register(nameof(Ragnarok_EnableSettings), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool Ragnarok_EnableSettings
        {
            get { return (bool)GetValue(Ragnarok_EnableSettingsProperty); }
            set { SetValue(Ragnarok_EnableSettingsProperty, value); }
        }

        public static readonly DependencyProperty Ragnarok_AllowMultipleTamedUnicornsProperty = DependencyProperty.Register(nameof(Ragnarok_AllowMultipleTamedUnicorns), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_Ragnarok, ServerProfileCategory.Rules, "AllowMultipleTamedUnicorns", ConditionedOn = nameof(Ragnarok_EnableSettings), ClearSectionIfEmpty = true)]
        public bool Ragnarok_AllowMultipleTamedUnicorns
        {
            get { return (bool)GetValue(Ragnarok_AllowMultipleTamedUnicornsProperty); }
            set { SetValue(Ragnarok_AllowMultipleTamedUnicornsProperty, value); }
        }

        public static readonly DependencyProperty Ragnarok_UnicornSpawnIntervalProperty = DependencyProperty.Register(nameof(Ragnarok_UnicornSpawnInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(24));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_Ragnarok, ServerProfileCategory.Rules, "UnicornSpawnInterval", ConditionedOn = nameof(Ragnarok_EnableSettings), ClearSectionIfEmpty = true)]
        public int Ragnarok_UnicornSpawnInterval
        {
            get { return (int)GetValue(Ragnarok_UnicornSpawnIntervalProperty); }
            set { SetValue(Ragnarok_UnicornSpawnIntervalProperty, value); }
        }

        public static readonly DependencyProperty Ragnarok_EnableVolcanoProperty = DependencyProperty.Register(nameof(Ragnarok_EnableVolcano), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_Ragnarok, ServerProfileCategory.Rules, "EnableVolcano", ConditionedOn = nameof(Ragnarok_EnableSettings), ClearSectionIfEmpty = true)]
        public bool Ragnarok_EnableVolcano
        {
            get { return (bool)GetValue(Ragnarok_EnableVolcanoProperty); }
            set { SetValue(Ragnarok_EnableVolcanoProperty, value); }
        }

        public static readonly DependencyProperty Ragnarok_VolcanoIntervalProperty = DependencyProperty.Register(nameof(Ragnarok_VolcanoInterval), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_Ragnarok, ServerProfileCategory.Rules, "VolcanoInterval", ConditionedOn = nameof(Ragnarok_EnableSettings), ClearSectionIfEmpty = true)]
        public float Ragnarok_VolcanoInterval
        {
            get { return (float)GetValue(Ragnarok_VolcanoIntervalProperty); }
            set { SetValue(Ragnarok_VolcanoIntervalProperty, value); }
        }

        public static readonly DependencyProperty Ragnarok_VolcanoIntensityProperty = DependencyProperty.Register(nameof(Ragnarok_VolcanoIntensity), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_Ragnarok, ServerProfileCategory.Rules, "VolcanoIntensity", ConditionedOn = nameof(Ragnarok_EnableSettings), ClearSectionIfEmpty = true)]
        public float Ragnarok_VolcanoIntensity
        {
            get { return (float)GetValue(Ragnarok_VolcanoIntensityProperty); }
            set { SetValue(Ragnarok_VolcanoIntensityProperty, value); }
        }

        public static readonly DependencyProperty Fjordur_EnableSettingsProperty = DependencyProperty.Register(nameof(Fjordur_EnableSettings), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool Fjordur_EnableSettings
        {
            get { return (bool)GetValue(Fjordur_EnableSettingsProperty); }
            set { SetValue(Fjordur_EnableSettingsProperty, value); }
        }

        public static readonly DependencyProperty UseFjordurTraversalBuffProperty = DependencyProperty.Register(nameof(UseFjordurTraversalBuff), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Rules, ConditionedOn = nameof(Fjordur_EnableSettings))]
        public bool UseFjordurTraversalBuff
        {
            get { return (bool)GetValue(UseFjordurTraversalBuffProperty); }
            set { SetValue(UseFjordurTraversalBuffProperty, value); }
        }


        public bool ClampItemStats
        {
            get
            {
                return ItemStatClamps_GenericQuality.HasValue
                    || ItemStatClamps_Armor.HasValue
                    || ItemStatClamps_MaxDurability.HasValue
                    || ItemStatClamps_WeaponDamagePercent.HasValue
                    || ItemStatClamps_WeaponClipAmmo.HasValue
                    || ItemStatClamps_HypothermalInsulation.HasValue
                    || ItemStatClamps_Weight.HasValue
                    || ItemStatClamps_HyperthermalInsulation.HasValue;
            }
        }

        public static readonly DependencyProperty ItemStatClamps_GenericQualityProperty = DependencyProperty.Register(nameof(ItemStatClamps_GenericQuality), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 0)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "ItemStatClamps[0]")]
        public NullableValue<int> ItemStatClamps_GenericQuality
        {
            get { return (NullableValue<int>)GetValue(ItemStatClamps_GenericQualityProperty); }
            set { SetValue(ItemStatClamps_GenericQualityProperty, value); }
        }

        public static readonly DependencyProperty ItemStatClamps_ArmorProperty = DependencyProperty.Register(nameof(ItemStatClamps_Armor), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 0)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "ItemStatClamps[1]")]
        public NullableValue<int> ItemStatClamps_Armor
        {
            get { return (NullableValue<int>)GetValue(ItemStatClamps_ArmorProperty); }
            set { SetValue(ItemStatClamps_ArmorProperty, value); }
        }

        public static readonly DependencyProperty ItemStatClamps_MaxDurabilityProperty = DependencyProperty.Register(nameof(ItemStatClamps_MaxDurability), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 0)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "ItemStatClamps[2]")]
        public NullableValue<int> ItemStatClamps_MaxDurability
        {
            get { return (NullableValue<int>)GetValue(ItemStatClamps_MaxDurabilityProperty); }
            set { SetValue(ItemStatClamps_MaxDurabilityProperty, value); }
        }

        public static readonly DependencyProperty ItemStatClamps_WeaponDamagePercentProperty = DependencyProperty.Register(nameof(ItemStatClamps_WeaponDamagePercent), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 0)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "ItemStatClamps[3]")]
        public NullableValue<int> ItemStatClamps_WeaponDamagePercent
        {
            get { return (NullableValue<int>)GetValue(ItemStatClamps_WeaponDamagePercentProperty); }
            set { SetValue(ItemStatClamps_WeaponDamagePercentProperty, value); }
        }

        public static readonly DependencyProperty ItemStatClamps_WeaponClipAmmoProperty = DependencyProperty.Register(nameof(ItemStatClamps_WeaponClipAmmo), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 0)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "ItemStatClamps[4]")]
        public NullableValue<int> ItemStatClamps_WeaponClipAmmo
        {
            get { return (NullableValue<int>)GetValue(ItemStatClamps_WeaponClipAmmoProperty); }
            set { SetValue(ItemStatClamps_WeaponClipAmmoProperty, value); }
        }

        public static readonly DependencyProperty ItemStatClamps_HypothermalInsulationProperty = DependencyProperty.Register(nameof(ItemStatClamps_HypothermalInsulation), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 0)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "ItemStatClamps[5]")]
        public NullableValue<int> ItemStatClamps_HypothermalInsulation
        {
            get { return (NullableValue<int>)GetValue(ItemStatClamps_HypothermalInsulationProperty); }
            set { SetValue(ItemStatClamps_HypothermalInsulationProperty, value); }
        }

        public static readonly DependencyProperty ItemStatClamps_WeightProperty = DependencyProperty.Register(nameof(ItemStatClamps_Weight), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 0)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "ItemStatClamps[6]")]
        public NullableValue<int> ItemStatClamps_Weight
        {
            get { return (NullableValue<int>)GetValue(ItemStatClamps_WeightProperty); }
            set { SetValue(ItemStatClamps_WeightProperty, value); }
        }

        public static readonly DependencyProperty ItemStatClamps_HyperthermalInsulationProperty = DependencyProperty.Register(nameof(ItemStatClamps_HyperthermalInsulation), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 0)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Rules, "ItemStatClamps[7]")]
        public NullableValue<int> ItemStatClamps_HyperthermalInsulation
        {
            get { return (NullableValue<int>)GetValue(ItemStatClamps_HyperthermalInsulationProperty); }
            set { SetValue(ItemStatClamps_HyperthermalInsulationProperty, value); }
        }
        #endregion

        #region Chat and Notifications
        public static readonly DependencyProperty EnableGlobalVoiceChatProperty = DependencyProperty.Register(nameof(EnableGlobalVoiceChat), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.ChatAndNotifications, "globalVoiceChat")]
        public bool EnableGlobalVoiceChat
        {
            get { return (bool)GetValue(EnableGlobalVoiceChatProperty); }
            set { SetValue(EnableGlobalVoiceChatProperty, value); }
        }

        public static readonly DependencyProperty EnableProximityChatProperty = DependencyProperty.Register(nameof(EnableProximityChat), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.ChatAndNotifications, "proximityChat")]
        public bool EnableProximityChat
        {
            get { return (bool)GetValue(EnableProximityChatProperty); }
            set { SetValue(EnableProximityChatProperty, value); }
        }

        public static readonly DependencyProperty EnablePlayerLeaveNotificationsProperty = DependencyProperty.Register(nameof(EnablePlayerLeaveNotifications), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.ChatAndNotifications, "alwaysNotifyPlayerLeft")]
        public bool EnablePlayerLeaveNotifications
        {
            get { return (bool)GetValue(EnablePlayerLeaveNotificationsProperty); }
            set { SetValue(EnablePlayerLeaveNotificationsProperty, value); }
        }

        public static readonly DependencyProperty EnablePlayerJoinedNotificationsProperty = DependencyProperty.Register(nameof(EnablePlayerJoinedNotifications), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.ChatAndNotifications, "DontAlwaysNotifyPlayerJoined")]
        public bool EnablePlayerJoinedNotifications
        {
            get { return (bool)GetValue(EnablePlayerJoinedNotificationsProperty); }
            set { SetValue(EnablePlayerJoinedNotificationsProperty, value); }
        }
        #endregion

        #region HUD and Visuals
        public static readonly DependencyProperty AllowCrosshairProperty = DependencyProperty.Register(nameof(AllowCrosshair), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.HudAndVisuals, "ServerCrosshair")]
        public bool AllowCrosshair
        {
            get { return (bool)GetValue(AllowCrosshairProperty); }
            set { SetValue(AllowCrosshairProperty, value); }
        }

        public static readonly DependencyProperty AllowHUDProperty = DependencyProperty.Register(nameof(AllowHUD), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.HudAndVisuals, "ServerForceNoHud", InvertBoolean = true)]
        public bool AllowHUD
        {
            get { return (bool)GetValue(AllowHUDProperty); }
            set { SetValue(AllowHUDProperty, value); }
        }

        public static readonly DependencyProperty AllowThirdPersonViewProperty = DependencyProperty.Register(nameof(AllowThirdPersonView), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.HudAndVisuals, "AllowThirdPersonPlayer")]
        public bool AllowThirdPersonView
        {
            get { return (bool)GetValue(AllowThirdPersonViewProperty); }
            set { SetValue(AllowThirdPersonViewProperty, value); }
        }

        public static readonly DependencyProperty AllowMapPlayerLocationProperty = DependencyProperty.Register(nameof(AllowMapPlayerLocation), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.HudAndVisuals, "ShowMapPlayerLocation")]
        public bool AllowMapPlayerLocation
        {
            get { return (bool)GetValue(AllowMapPlayerLocationProperty); }
            set { SetValue(AllowMapPlayerLocationProperty, value); }
        }

        public static readonly DependencyProperty AllowPVPGammaProperty = DependencyProperty.Register(nameof(AllowPVPGamma), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.HudAndVisuals, "EnablePVPGamma")]
        public bool AllowPVPGamma
        {
            get { return (bool)GetValue(AllowPVPGammaProperty); }
            set { SetValue(AllowPVPGammaProperty, value); }
        }

        public static readonly DependencyProperty AllowPvEGammaProperty = DependencyProperty.Register(nameof(AllowPvEGamma), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.HudAndVisuals, "DisablePvEGamma", InvertBoolean = true)]
        public bool AllowPvEGamma
        {
            get { return (bool)GetValue(AllowPvEGammaProperty); }
            set { SetValue(AllowPvEGammaProperty, value); }
        }

        public static readonly DependencyProperty ShowFloatingDamageTextProperty = DependencyProperty.Register(nameof(ShowFloatingDamageText), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.HudAndVisuals)]
        public bool ShowFloatingDamageText
        {
            get { return (bool)GetValue(ShowFloatingDamageTextProperty); }
            set { SetValue(ShowFloatingDamageTextProperty, value); }
        }

        public static readonly DependencyProperty AllowHitMarkersProperty = DependencyProperty.Register(nameof(AllowHitMarkers), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.HudAndVisuals)]
        public bool AllowHitMarkers
        {
            get { return (bool)GetValue(AllowHitMarkersProperty); }
            set { SetValue(AllowHitMarkersProperty, value); }
        }
        #endregion

        #region Player Settings
        public static readonly DependencyProperty EnableFlyerCarryProperty = DependencyProperty.Register(nameof(EnableFlyerCarry), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Players, "AllowFlyerCarryPVE")]
        public bool EnableFlyerCarry
        {
            get { return (bool)GetValue(EnableFlyerCarryProperty); }
            set { SetValue(EnableFlyerCarryProperty, value); }
        }

        public static readonly DependencyProperty XPMultiplierProperty = DependencyProperty.Register(nameof(XPMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float XPMultiplier
        {
            get { return (float)GetValue(XPMultiplierProperty); }
            set { SetValue(XPMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PlayerDamageMultiplierProperty = DependencyProperty.Register(nameof(PlayerDamageMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float PlayerDamageMultiplier
        {
            get { return (float)GetValue(PlayerDamageMultiplierProperty); }
            set { SetValue(PlayerDamageMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PlayerResistanceMultiplierProperty = DependencyProperty.Register(nameof(PlayerResistanceMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float PlayerResistanceMultiplier
        {
            get { return (float)GetValue(PlayerResistanceMultiplierProperty); }
            set { SetValue(PlayerResistanceMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PlayerCharacterWaterDrainMultiplierProperty = DependencyProperty.Register(nameof(PlayerCharacterWaterDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float PlayerCharacterWaterDrainMultiplier
        {
            get { return (float)GetValue(PlayerCharacterWaterDrainMultiplierProperty); }
            set { SetValue(PlayerCharacterWaterDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PlayerCharacterFoodDrainMultiplierProperty = DependencyProperty.Register(nameof(PlayerCharacterFoodDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float PlayerCharacterFoodDrainMultiplier
        {
            get { return (float)GetValue(PlayerCharacterFoodDrainMultiplierProperty); }
            set { SetValue(PlayerCharacterFoodDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PlayerCharacterStaminaDrainMultiplierProperty = DependencyProperty.Register(nameof(PlayerCharacterStaminaDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float PlayerCharacterStaminaDrainMultiplier
        {
            get { return (float)GetValue(PlayerCharacterStaminaDrainMultiplierProperty); }
            set { SetValue(PlayerCharacterStaminaDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PlayerCharacterHealthRecoveryMultiplierProperty = DependencyProperty.Register(nameof(PlayerCharacterHealthRecoveryMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float PlayerCharacterHealthRecoveryMultiplier
        {
            get { return (float)GetValue(PlayerCharacterHealthRecoveryMultiplierProperty); }
            set { SetValue(PlayerCharacterHealthRecoveryMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PlayerHarvestingDamageMultiplierProperty = DependencyProperty.Register(nameof(PlayerHarvestingDamageMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float PlayerHarvestingDamageMultiplier
        {
            get { return (float)GetValue(PlayerHarvestingDamageMultiplierProperty); }
            set { SetValue(PlayerHarvestingDamageMultiplierProperty, value); }
        }

        public static readonly DependencyProperty CraftingSkillBonusMultiplierProperty = DependencyProperty.Register(nameof(CraftingSkillBonusMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float CraftingSkillBonusMultiplier
        {
            get { return (float)GetValue(CraftingSkillBonusMultiplierProperty); }
            set { SetValue(CraftingSkillBonusMultiplierProperty, value); }
        }

        public static readonly DependencyProperty MaxFallSpeedMultiplierProperty = DependencyProperty.Register(nameof(MaxFallSpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Players, WriteIfNotValue = 1.0f)]
        public float MaxFallSpeedMultiplier
        {
            get { return (float)GetValue(MaxFallSpeedMultiplierProperty); }
            set { SetValue(MaxFallSpeedMultiplierProperty, value); }
        }



        public static readonly DependencyProperty PlayerBaseStatMultipliersProperty = DependencyProperty.Register(nameof(PlayerBaseStatMultipliers), typeof(StatsMultiplierFloatArray), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Players)]
        public StatsMultiplierFloatArray PlayerBaseStatMultipliers
        {
            get { return (StatsMultiplierFloatArray)GetValue(PlayerBaseStatMultipliersProperty); }
            set { SetValue(PlayerBaseStatMultipliersProperty, value); }
        }

        public static readonly DependencyProperty PerLevelStatsMultiplier_PlayerProperty = DependencyProperty.Register(nameof(PerLevelStatsMultiplier_Player), typeof(StatsMultiplierFloatArray), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Players)]
        public StatsMultiplierFloatArray PerLevelStatsMultiplier_Player
        {
            get { return (StatsMultiplierFloatArray)GetValue(PerLevelStatsMultiplier_PlayerProperty); }
            set { SetValue(PerLevelStatsMultiplier_PlayerProperty, value); }
        }
        #endregion

        #region Dino Settings
        public static readonly DependencyProperty DinoDamageMultiplierProperty = DependencyProperty.Register(nameof(DinoDamageMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float DinoDamageMultiplier
        {
            get { return (float)GetValue(DinoDamageMultiplierProperty); }
            set { SetValue(DinoDamageMultiplierProperty, value); }
        }

        public static readonly DependencyProperty TamedDinoDamageMultiplierProperty = DependencyProperty.Register(nameof(TamedDinoDamageMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float TamedDinoDamageMultiplier
        {
            get { return (float)GetValue(TamedDinoDamageMultiplierProperty); }
            set { SetValue(TamedDinoDamageMultiplierProperty, value); }
        }

        public static readonly DependencyProperty DinoResistanceMultiplierProperty = DependencyProperty.Register(nameof(DinoResistanceMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float DinoResistanceMultiplier
        {
            get { return (float)GetValue(DinoResistanceMultiplierProperty); }
            set { SetValue(DinoResistanceMultiplierProperty, value); }
        }

        public static readonly DependencyProperty TamedDinoResistanceMultiplierProperty = DependencyProperty.Register(nameof(TamedDinoResistanceMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float TamedDinoResistanceMultiplier
        {
            get { return (float)GetValue(TamedDinoResistanceMultiplierProperty); }
            set { SetValue(TamedDinoResistanceMultiplierProperty, value); }
        }

        public static readonly DependencyProperty DinoCharacterFoodDrainMultiplierProperty = DependencyProperty.Register(nameof(DinoCharacterFoodDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float DinoCharacterFoodDrainMultiplier
        {
            get { return (float)GetValue(DinoCharacterFoodDrainMultiplierProperty); }
            set { SetValue(DinoCharacterFoodDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty DinoCharacterStaminaDrainMultiplierProperty = DependencyProperty.Register(nameof(DinoCharacterStaminaDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float DinoCharacterStaminaDrainMultiplier
        {
            get { return (float)GetValue(DinoCharacterStaminaDrainMultiplierProperty); }
            set { SetValue(DinoCharacterStaminaDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty DinoCharacterHealthRecoveryMultiplierProperty = DependencyProperty.Register(nameof(DinoCharacterHealthRecoveryMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float DinoCharacterHealthRecoveryMultiplier
        {
            get { return (float)GetValue(DinoCharacterHealthRecoveryMultiplierProperty); }
            set { SetValue(DinoCharacterHealthRecoveryMultiplierProperty, value); }
        }

        public static readonly DependencyProperty DinoHarvestingDamageMultiplierProperty = DependencyProperty.Register(nameof(DinoHarvestingDamageMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(3.2f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 3.0f)]
        public float DinoHarvestingDamageMultiplier
        {
            get { return (float)GetValue(DinoHarvestingDamageMultiplierProperty); }
            set { SetValue(DinoHarvestingDamageMultiplierProperty, value); }
        }

        public static readonly DependencyProperty DinoTurretDamageMultiplierProperty = DependencyProperty.Register(nameof(DinoTurretDamageMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float DinoTurretDamageMultiplier
        {
            get { return (float)GetValue(DinoTurretDamageMultiplierProperty); }
            set { SetValue(DinoTurretDamageMultiplierProperty, value); }
        }

        public static readonly DependencyProperty AllowRaidDinoFeedingProperty = DependencyProperty.Register(nameof(AllowRaidDinoFeeding), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos)]
        public bool AllowRaidDinoFeeding
        {
            get { return (bool)GetValue(AllowRaidDinoFeedingProperty); }
            set { SetValue(AllowRaidDinoFeedingProperty, value); }
        }

        public static readonly DependencyProperty RaidDinoCharacterFoodDrainMultiplierProperty = DependencyProperty.Register(nameof(RaidDinoCharacterFoodDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float RaidDinoCharacterFoodDrainMultiplier
        {
            get { return (float)GetValue(RaidDinoCharacterFoodDrainMultiplierProperty); }
            set { SetValue(RaidDinoCharacterFoodDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty EnableAllowCaveFlyersProperty = DependencyProperty.Register(nameof(EnableAllowCaveFlyers), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableAllowCaveFlyers
        {
            get { return (bool)GetValue(EnableAllowCaveFlyersProperty); }
            set { SetValue(EnableAllowCaveFlyersProperty, value); }
        }

        public static readonly DependencyProperty AllowFlyingStaminaRecoveryProperty = DependencyProperty.Register(nameof(AllowFlyingStaminaRecovery), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, ConditionedOn = nameof(AllowFlyingStaminaRecovery))]
        public bool AllowFlyingStaminaRecovery
        {
            get { return (bool)GetValue(AllowFlyingStaminaRecoveryProperty); }
            set { SetValue(AllowFlyingStaminaRecoveryProperty, value); }
        }

        public static readonly DependencyProperty AllowFlyerSpeedLevelingProperty = DependencyProperty.Register(nameof(AllowFlyerSpeedLeveling), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, "bAllowFlyerSpeedLeveling", ConditionedOn = nameof(AllowFlyerSpeedLeveling))]
        public bool AllowFlyerSpeedLeveling
        {
            get { return (bool)GetValue(AllowFlyerSpeedLevelingProperty); }
            set { SetValue(AllowFlyerSpeedLevelingProperty, value); }
        }

        public static readonly DependencyProperty PreventMateBoostProperty = DependencyProperty.Register(nameof(PreventMateBoost), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, ConditionedOn = nameof(PreventMateBoost))]
        public bool PreventMateBoost
        {
            get { return (bool)GetValue(PreventMateBoostProperty); }
            set { SetValue(PreventMateBoostProperty, value); }
        }

        public static readonly DependencyProperty DisableDinoDecayPvEProperty = DependencyProperty.Register(nameof(DisableDinoDecayPvE), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos)]
        public bool DisableDinoDecayPvE
        {
            get { return (bool)GetValue(DisableDinoDecayPvEProperty); }
            set { SetValue(DisableDinoDecayPvEProperty, value); }
        }

        public static readonly DependencyProperty DisableDinoDecayPvPProperty = DependencyProperty.Register(nameof(DisableDinoDecayPvP), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, "PvPDinoDecay", InvertBoolean=true)]
        public bool DisableDinoDecayPvP
        {
            get { return (bool)GetValue(DisableDinoDecayPvPProperty); }
            set { SetValue(DisableDinoDecayPvPProperty, value); }
        }

        public static readonly DependencyProperty AutoDestroyDecayedDinosProperty = DependencyProperty.Register(nameof(AutoDestroyDecayedDinos), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos)]
        public bool AutoDestroyDecayedDinos
        {
            get { return (bool)GetValue(AutoDestroyDecayedDinosProperty); }
            set { SetValue(AutoDestroyDecayedDinosProperty, value); }
        }

        public static readonly DependencyProperty UseDinoLevelUpAnimationsProperty = DependencyProperty.Register(nameof(UseDinoLevelUpAnimations), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, "bUseDinoLevelUpAnimations")]
        public bool UseDinoLevelUpAnimations
        {
            get { return (bool)GetValue(UseDinoLevelUpAnimationsProperty); }
            set { SetValue(UseDinoLevelUpAnimationsProperty, value); }
        }

        public static readonly DependencyProperty PvEDinoDecayPeriodMultiplierProperty = DependencyProperty.Register(nameof(PvEDinoDecayPeriodMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float PvEDinoDecayPeriodMultiplier
        {
            get { return (float)GetValue(PvEDinoDecayPeriodMultiplierProperty); }
            set { SetValue(PvEDinoDecayPeriodMultiplierProperty, value); }
        }

        public static readonly DependencyProperty AllowMultipleAttachedC4Property = DependencyProperty.Register(nameof(AllowMultipleAttachedC4), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, ConditionedOn = nameof(AllowMultipleAttachedC4))]
        public bool AllowMultipleAttachedC4
        {
            get { return (bool)GetValue(AllowMultipleAttachedC4Property); }
            set { SetValue(AllowMultipleAttachedC4Property, value); }
        }

        public static readonly DependencyProperty AllowUnclaimDinosProperty = DependencyProperty.Register(nameof(AllowUnclaimDinos), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, "bAllowUnclaimDinos")]
        public bool AllowUnclaimDinos
        {
            get { return (bool)GetValue(AllowUnclaimDinosProperty); }
            set { SetValue(AllowUnclaimDinosProperty, value); }
        }

        public static readonly DependencyProperty DisableDinoRidingProperty = DependencyProperty.Register(nameof(DisableDinoRiding), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, "bDisableDinoRiding", ConditionedOn = nameof(DisableDinoRiding))]
        public bool DisableDinoRiding
        {
            get { return (bool)GetValue(DisableDinoRidingProperty); }
            set { SetValue(DisableDinoRidingProperty, value); }
        }

        public static readonly DependencyProperty DisableDinoTamingProperty = DependencyProperty.Register(nameof(DisableDinoTaming), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, "bDisableDinoTaming", ConditionedOn = nameof(DisableDinoTaming))]
        public bool DisableDinoTaming
        {
            get { return (bool)GetValue(DisableDinoTamingProperty); }
            set { SetValue(DisableDinoTamingProperty, value); }
        }

        public static readonly DependencyProperty DisableDinoBreedingProperty = DependencyProperty.Register(nameof(DisableDinoBreeding), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, "bDisableDinoBreeding", ConditionedOn = nameof(DisableDinoBreeding))]
        public bool DisableDinoBreeding
        {
            get { return (bool)GetValue(DisableDinoBreedingProperty); }
            set { SetValue(DisableDinoBreedingProperty, value); }
        }

        public static readonly DependencyProperty MaxTamedDinosProperty = DependencyProperty.Register(nameof(MaxTamedDinos), typeof(int), typeof(ServerProfile), new PropertyMetadata(5000));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos)]
        public int MaxTamedDinos
        {
            get { return (int)GetValue(MaxTamedDinosProperty); }
            set { SetValue(MaxTamedDinosProperty, value); }
        }

        public static readonly DependencyProperty MaxPersonalTamedDinosProperty = DependencyProperty.Register(nameof(MaxPersonalTamedDinos), typeof(float), typeof(ServerProfile), new PropertyMetadata(40.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos)]
        public float MaxPersonalTamedDinos
        {
            get { return (float)GetValue(MaxPersonalTamedDinosProperty); }
            set { SetValue(MaxPersonalTamedDinosProperty, value); }
        }

        public static readonly DependencyProperty PersonalTamedDinosSaddleStructureCostProperty = DependencyProperty.Register(nameof(PersonalTamedDinosSaddleStructureCost), typeof(int), typeof(ServerProfile), new PropertyMetadata(19));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos)]
        public int PersonalTamedDinosSaddleStructureCost
        {
            get { return (int)GetValue(PersonalTamedDinosSaddleStructureCostProperty); }
            set { SetValue(PersonalTamedDinosSaddleStructureCostProperty, value); }
        }

        public static readonly DependencyProperty UseTameLimitForStructuresOnlyProperty = DependencyProperty.Register(nameof(UseTameLimitForStructuresOnly), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, "bUseTameLimitForStructuresOnly", ConditionedOn = nameof(UseTameLimitForStructuresOnly))]
        public bool UseTameLimitForStructuresOnly
        {
            get { return (bool)GetValue(UseTameLimitForStructuresOnlyProperty); }
            set { SetValue(UseTameLimitForStructuresOnlyProperty, value); }
        }

        public static readonly DependencyProperty EnableForceCanRideFliersProperty = DependencyProperty.Register(nameof(EnableForceCanRideFliers), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool EnableForceCanRideFliers
        {
            get { return (bool)GetValue(EnableForceCanRideFliersProperty); }
            set { SetValue(EnableForceCanRideFliersProperty, value); }
        }

        public static readonly DependencyProperty ForceCanRideFliersProperty = DependencyProperty.Register(nameof(ForceCanRideFliers), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos, "bForceCanRideFliers", ConditionedOn = nameof(EnableForceCanRideFliers))]
        public bool ForceCanRideFliers
        {
            get { return (bool)GetValue(ForceCanRideFliersProperty); }
            set { SetValue(ForceCanRideFliersProperty, value); }
        }

        public static readonly DependencyProperty MatingIntervalMultiplierProperty = DependencyProperty.Register(nameof(MatingIntervalMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float MatingIntervalMultiplier
        {
            get { return (float)GetValue(MatingIntervalMultiplierProperty); }
            set { SetValue(MatingIntervalMultiplierProperty, value); }
        }

        public static readonly DependencyProperty MatingSpeedMultiplierProperty = DependencyProperty.Register(nameof(MatingSpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float MatingSpeedMultiplier
        {
            get { return (float)GetValue(MatingSpeedMultiplierProperty); }
            set { SetValue(MatingSpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty EggHatchSpeedMultiplierProperty = DependencyProperty.Register(nameof(EggHatchSpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float EggHatchSpeedMultiplier
        {
            get { return (float)GetValue(EggHatchSpeedMultiplierProperty); }
            set { SetValue(EggHatchSpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty BabyMatureSpeedMultiplierProperty = DependencyProperty.Register(nameof(BabyMatureSpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float BabyMatureSpeedMultiplier
        {
            get { return (float)GetValue(BabyMatureSpeedMultiplierProperty); }
            set { SetValue(BabyMatureSpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty BabyFoodConsumptionSpeedMultiplierProperty = DependencyProperty.Register(nameof(BabyFoodConsumptionSpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float BabyFoodConsumptionSpeedMultiplier
        {
            get { return (float)GetValue(BabyFoodConsumptionSpeedMultiplierProperty); }
            set { SetValue(BabyFoodConsumptionSpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty DisableImprintDinoBuffProperty = DependencyProperty.Register(nameof(DisableImprintDinoBuff), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos)]
        public bool DisableImprintDinoBuff
        {
            get { return (bool)GetValue(DisableImprintDinoBuffProperty); }
            set { SetValue(DisableImprintDinoBuffProperty, value); }
        }

        public static readonly DependencyProperty AllowAnyoneBabyImprintCuddleProperty = DependencyProperty.Register(nameof(AllowAnyoneBabyImprintCuddle), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Dinos)]
        public bool AllowAnyoneBabyImprintCuddle
        {
            get { return (bool)GetValue(AllowAnyoneBabyImprintCuddleProperty); }
            set { SetValue(AllowAnyoneBabyImprintCuddleProperty, value); }
        }

        public static readonly DependencyProperty BabyImprintingStatScaleMultiplierProperty = DependencyProperty.Register(nameof(BabyImprintingStatScaleMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float BabyImprintingStatScaleMultiplier
        {
            get { return (float)GetValue(BabyImprintingStatScaleMultiplierProperty); }
            set { SetValue(BabyImprintingStatScaleMultiplierProperty, value); }
        }

        public static readonly DependencyProperty BabyImprintAmountMultiplierProperty = DependencyProperty.Register(nameof(BabyImprintAmountMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float BabyImprintAmountMultiplier
        {
            get { return (float)GetValue(BabyImprintAmountMultiplierProperty); }
            set { SetValue(BabyImprintAmountMultiplierProperty, value); }
        }

        public static readonly DependencyProperty BabyCuddleIntervalMultiplierProperty = DependencyProperty.Register(nameof(BabyCuddleIntervalMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float BabyCuddleIntervalMultiplier
        {
            get { return (float)GetValue(BabyCuddleIntervalMultiplierProperty); }
            set { SetValue(BabyCuddleIntervalMultiplierProperty, value); }
        }

        public static readonly DependencyProperty BabyCuddleGracePeriodMultiplierProperty = DependencyProperty.Register(nameof(BabyCuddleGracePeriodMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float BabyCuddleGracePeriodMultiplier
        {
            get { return (float)GetValue(BabyCuddleGracePeriodMultiplierProperty); }
            set { SetValue(BabyCuddleGracePeriodMultiplierProperty, value); }
        }

        public static readonly DependencyProperty BabyCuddleLoseImprintQualitySpeedMultiplierProperty = DependencyProperty.Register(nameof(BabyCuddleLoseImprintQualitySpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float BabyCuddleLoseImprintQualitySpeedMultiplier
        {
            get { return (float)GetValue(BabyCuddleLoseImprintQualitySpeedMultiplierProperty); }
            set { SetValue(BabyCuddleLoseImprintQualitySpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty ImprintlimitProperty = DependencyProperty.Register(nameof(Imprintlimit), typeof(NullableValue<int>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<int>(false, 101)));
        [DataMember]
        public NullableValue<int> Imprintlimit
        {
            get { return (NullableValue<int>)GetValue(ImprintlimitProperty); }
            set { SetValue(ImprintlimitProperty, value); }
        }

        public static readonly DependencyProperty WildDinoCharacterFoodDrainMultiplierProperty = DependencyProperty.Register(nameof(WildDinoCharacterFoodDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float WildDinoCharacterFoodDrainMultiplier
        {
            get { return (float)GetValue(WildDinoCharacterFoodDrainMultiplierProperty); }
            set { SetValue(WildDinoCharacterFoodDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty TamedDinoCharacterFoodDrainMultiplierProperty = DependencyProperty.Register(nameof(TamedDinoCharacterFoodDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float TamedDinoCharacterFoodDrainMultiplier
        {
            get { return (float)GetValue(TamedDinoCharacterFoodDrainMultiplierProperty); }
            set { SetValue(TamedDinoCharacterFoodDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty WildDinoTorporDrainMultiplierProperty = DependencyProperty.Register(nameof(WildDinoTorporDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float WildDinoTorporDrainMultiplier
        {
            get { return (float)GetValue(WildDinoTorporDrainMultiplierProperty); }
            set { SetValue(WildDinoTorporDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty TamedDinoTorporDrainMultiplierProperty = DependencyProperty.Register(nameof(TamedDinoTorporDrainMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float TamedDinoTorporDrainMultiplier
        {
            get { return (float)GetValue(TamedDinoTorporDrainMultiplierProperty); }
            set { SetValue(TamedDinoTorporDrainMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PassiveTameIntervalMultiplierProperty = DependencyProperty.Register(nameof(PassiveTameIntervalMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos, WriteIfNotValue = 1.0f)]
        public float PassiveTameIntervalMultiplier
        {
            get { return (float)GetValue(PassiveTameIntervalMultiplierProperty); }
            set { SetValue(PassiveTameIntervalMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PerLevelStatsMultiplier_DinoWildProperty = DependencyProperty.Register(nameof(PerLevelStatsMultiplier_DinoWild), typeof(StatsMultiplierFloatArray), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public StatsMultiplierFloatArray PerLevelStatsMultiplier_DinoWild
        {
            get { return (StatsMultiplierFloatArray)GetValue(PerLevelStatsMultiplier_DinoWildProperty); }
            set { SetValue(PerLevelStatsMultiplier_DinoWildProperty, value); }
        }

        public static readonly DependencyProperty PerLevelStatsMultiplier_DinoTamedProperty = DependencyProperty.Register(nameof(PerLevelStatsMultiplier_DinoTamed), typeof(StatsMultiplierFloatArray), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public StatsMultiplierFloatArray PerLevelStatsMultiplier_DinoTamed
        {
            get { return (StatsMultiplierFloatArray)GetValue(PerLevelStatsMultiplier_DinoTamedProperty); }
            set { SetValue(PerLevelStatsMultiplier_DinoTamedProperty, value); }
        }

        public static readonly DependencyProperty PerLevelStatsMultiplier_DinoTamed_AddProperty = DependencyProperty.Register(nameof(PerLevelStatsMultiplier_DinoTamed_Add), typeof(StatsMultiplierFloatArray), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public StatsMultiplierFloatArray PerLevelStatsMultiplier_DinoTamed_Add
        {
            get { return (StatsMultiplierFloatArray)GetValue(PerLevelStatsMultiplier_DinoTamed_AddProperty); }
            set { SetValue(PerLevelStatsMultiplier_DinoTamed_AddProperty, value); }
        }

        public static readonly DependencyProperty PerLevelStatsMultiplier_DinoTamed_AffinityProperty = DependencyProperty.Register(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity), typeof(StatsMultiplierFloatArray), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public StatsMultiplierFloatArray PerLevelStatsMultiplier_DinoTamed_Affinity
        {
            get { return (StatsMultiplierFloatArray)GetValue(PerLevelStatsMultiplier_DinoTamed_AffinityProperty); }
            set { SetValue(PerLevelStatsMultiplier_DinoTamed_AffinityProperty, value); }
        }

        public static readonly DependencyProperty MutagenLevelBoostProperty = DependencyProperty.Register(nameof(MutagenLevelBoost), typeof(StatsMultiplierIntegerArray), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public StatsMultiplierIntegerArray MutagenLevelBoost
        {
            get { return (StatsMultiplierIntegerArray)GetValue(MutagenLevelBoostProperty); }
            set { SetValue(MutagenLevelBoostProperty, value); }
        }

        public static readonly DependencyProperty MutagenLevelBoost_BredProperty = DependencyProperty.Register(nameof(MutagenLevelBoost_Bred), typeof(StatsMultiplierIntegerArray), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public StatsMultiplierIntegerArray MutagenLevelBoost_Bred
        {
            get { return (StatsMultiplierIntegerArray)GetValue(MutagenLevelBoost_BredProperty); }
            set { SetValue(MutagenLevelBoost_BredProperty, value); }
        }

        public static readonly DependencyProperty DinoSpawnsProperty = DependencyProperty.Register(nameof(DinoSpawnWeightMultipliers), typeof(AggregateIniValueList<DinoSpawn>), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public AggregateIniValueList<DinoSpawn> DinoSpawnWeightMultipliers
        {
            get { return (AggregateIniValueList<DinoSpawn>)GetValue(DinoSpawnsProperty); }
            set { SetValue(DinoSpawnsProperty, value); }
        }

        public static readonly DependencyProperty TamedDinoClassDamageMultipliersProperty = DependencyProperty.Register(nameof(TamedDinoClassDamageMultipliers), typeof(AggregateIniValueList<ClassMultiplier>), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public AggregateIniValueList<ClassMultiplier> TamedDinoClassDamageMultipliers
        {
            get { return (AggregateIniValueList<ClassMultiplier>)GetValue(TamedDinoClassDamageMultipliersProperty); }
            set { SetValue(TamedDinoClassDamageMultipliersProperty, value); }
        }

        public static readonly DependencyProperty TamedDinoClassResistanceMultipliersProperty = DependencyProperty.Register(nameof(TamedDinoClassResistanceMultipliers), typeof(AggregateIniValueList<ClassMultiplier>), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public AggregateIniValueList<ClassMultiplier> TamedDinoClassResistanceMultipliers
        {
            get { return (AggregateIniValueList<ClassMultiplier>)GetValue(TamedDinoClassResistanceMultipliersProperty); }
            set { SetValue(TamedDinoClassResistanceMultipliersProperty, value); }
        }

        public static readonly DependencyProperty DinoClassDamageMultipliersProperty = DependencyProperty.Register(nameof(DinoClassDamageMultipliers), typeof(AggregateIniValueList<ClassMultiplier>), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public AggregateIniValueList<ClassMultiplier> DinoClassDamageMultipliers
        {
            get { return (AggregateIniValueList<ClassMultiplier>)GetValue(DinoClassDamageMultipliersProperty); }
            set { SetValue(DinoClassDamageMultipliersProperty, value); }
        }

        public static readonly DependencyProperty DinoClassResistanceMultipliersProperty = DependencyProperty.Register(nameof(DinoClassResistanceMultipliers), typeof(AggregateIniValueList<ClassMultiplier>), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public AggregateIniValueList<ClassMultiplier> DinoClassResistanceMultipliers
        {
            get { return (AggregateIniValueList<ClassMultiplier>)GetValue(DinoClassResistanceMultipliersProperty); }
            set { SetValue(DinoClassResistanceMultipliersProperty, value); }
        }

        public static readonly DependencyProperty NPCReplacementsProperty = DependencyProperty.Register(nameof(NPCReplacements), typeof(AggregateIniValueList<NPCReplacement>), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public AggregateIniValueList<NPCReplacement> NPCReplacements
        {
            get { return (AggregateIniValueList<NPCReplacement>)GetValue(NPCReplacementsProperty); }
            set { SetValue(NPCReplacementsProperty, value); }
        }

        public static readonly DependencyProperty PreventDinoTameClassNamesProperty = DependencyProperty.Register(nameof(PreventDinoTameClassNames), typeof(StringIniValueList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public StringIniValueList PreventDinoTameClassNames
        {
            get { return (StringIniValueList)GetValue(PreventDinoTameClassNamesProperty); }
            set { SetValue(PreventDinoTameClassNamesProperty, value); }
        }

        public static readonly DependencyProperty PreventBreedingForClassNamesProperty = DependencyProperty.Register(nameof(PreventBreedingForClassNames), typeof(StringIniValueList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Dinos)]
        public StringIniValueList PreventBreedingForClassNames
        {
            get { return (StringIniValueList)GetValue(PreventBreedingForClassNamesProperty); }
            set { SetValue(PreventBreedingForClassNamesProperty, value); }
        }

        public static readonly DependencyProperty DinoSettingsProperty = DependencyProperty.Register(nameof(DinoSettings), typeof(DinoSettingsList), typeof(ServerProfile), new PropertyMetadata(null));
        public DinoSettingsList DinoSettings
        {
            get { return (DinoSettingsList)GetValue(DinoSettingsProperty); }
            set { SetValue(DinoSettingsProperty, value); }
        }
        #endregion

        #region Environment
        public static readonly DependencyProperty DinoCountMultiplierProperty = DependencyProperty.Register(nameof(DinoCountMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float DinoCountMultiplier
        {
            get { return (float)GetValue(DinoCountMultiplierProperty); }
            set { SetValue(DinoCountMultiplierProperty, value); }
        }

        public static readonly DependencyProperty TamingSpeedMultiplierProperty = DependencyProperty.Register(nameof(TamingSpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float TamingSpeedMultiplier
        {
            get { return (float)GetValue(TamingSpeedMultiplierProperty); }
            set { SetValue(TamingSpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty HarvestAmountMultiplierProperty = DependencyProperty.Register(nameof(HarvestAmountMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float HarvestAmountMultiplier
        {
            get { return (float)GetValue(HarvestAmountMultiplierProperty); }
            set { SetValue(HarvestAmountMultiplierProperty, value); }
        }

        public static readonly DependencyProperty ResourcesRespawnPeriodMultiplierProperty = DependencyProperty.Register(nameof(ResourcesRespawnPeriodMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float ResourcesRespawnPeriodMultiplier
        {
            get { return (float)GetValue(ResourcesRespawnPeriodMultiplierProperty); }
            set { SetValue(ResourcesRespawnPeriodMultiplierProperty, value); }
        }

        public static readonly DependencyProperty ResourceNoReplenishRadiusPlayersProperty = DependencyProperty.Register(nameof(ResourceNoReplenishRadiusPlayers), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float ResourceNoReplenishRadiusPlayers
        {
            get { return (float)GetValue(ResourceNoReplenishRadiusPlayersProperty); }
            set { SetValue(ResourceNoReplenishRadiusPlayersProperty, value); }
        }

        public static readonly DependencyProperty ResourceNoReplenishRadiusStructuresProperty = DependencyProperty.Register(nameof(ResourceNoReplenishRadiusStructures), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float ResourceNoReplenishRadiusStructures
        {
            get { return (float)GetValue(ResourceNoReplenishRadiusStructuresProperty); }
            set { SetValue(ResourceNoReplenishRadiusStructuresProperty, value); }
        }

        public static readonly DependencyProperty HarvestHealthMultiplierProperty = DependencyProperty.Register(nameof(HarvestHealthMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float HarvestHealthMultiplier
        {
            get { return (float)GetValue(HarvestHealthMultiplierProperty); }
            set { SetValue(HarvestHealthMultiplierProperty, value); }
        }

        public static readonly DependencyProperty UseOptimizedHarvestingHealthProperty = DependencyProperty.Register(nameof(UseOptimizedHarvestingHealth), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment)]
        public bool UseOptimizedHarvestingHealth
        {
            get { return (bool)GetValue(UseOptimizedHarvestingHealthProperty); }
            set { SetValue(UseOptimizedHarvestingHealthProperty, value); }
        }

        public static readonly DependencyProperty ClampResourceHarvestDamageProperty = DependencyProperty.Register(nameof(ClampResourceHarvestDamage), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment)]
        public bool ClampResourceHarvestDamage
        {
            get { return (bool)GetValue(ClampResourceHarvestDamageProperty); }
            set { SetValue(ClampResourceHarvestDamageProperty, value); }
        }

        public static readonly DependencyProperty ClampItemSpoilingTimesProperty = DependencyProperty.Register(nameof(ClampItemSpoilingTimes), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment)]
        public bool ClampItemSpoilingTimes
        {
            get { return (bool)GetValue(ClampItemSpoilingTimesProperty); }
            set { SetValue(ClampItemSpoilingTimesProperty, value); }
        }

        public static readonly DependencyProperty BaseTemperatureMultiplierProperty = DependencyProperty.Register(nameof(BaseTemperatureMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float BaseTemperatureMultiplier
        {
            get { return (float)GetValue(BaseTemperatureMultiplierProperty); }
            set { SetValue(BaseTemperatureMultiplierProperty, value); }
        }

        public static readonly DependencyProperty DayCycleSpeedScaleProperty = DependencyProperty.Register(nameof(DayCycleSpeedScale), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float DayCycleSpeedScale
        {
            get { return (float)GetValue(DayCycleSpeedScaleProperty); }
            set { SetValue(DayCycleSpeedScaleProperty, value); }
        }

        public static readonly DependencyProperty DayTimeSpeedScaleProperty = DependencyProperty.Register(nameof(DayTimeSpeedScale), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float DayTimeSpeedScale
        {
            get { return (float)GetValue(DayTimeSpeedScaleProperty); }
            set { SetValue(DayTimeSpeedScaleProperty, value); }
        }

        public static readonly DependencyProperty NightTimeSpeedScaleProperty = DependencyProperty.Register(nameof(NightTimeSpeedScale), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float NightTimeSpeedScale
        {
            get { return (float)GetValue(NightTimeSpeedScaleProperty); }
            set { SetValue(NightTimeSpeedScaleProperty, value); }
        }

        public static readonly DependencyProperty DisableWeatherFogProperty = DependencyProperty.Register(nameof(DisableWeatherFog), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Environment, ConditionedOn = nameof(DisableWeatherFog))]
        public bool DisableWeatherFog
        {
            get { return (bool)GetValue(DisableWeatherFogProperty); }
            set { SetValue(DisableWeatherFogProperty, value); }
        }

        public static readonly DependencyProperty GlobalSpoilingTimeMultiplierProperty = DependencyProperty.Register(nameof(GlobalSpoilingTimeMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float GlobalSpoilingTimeMultiplier
        {
            get { return (float)GetValue(GlobalSpoilingTimeMultiplierProperty); }
            set { SetValue(GlobalSpoilingTimeMultiplierProperty, value); }
        }

        public static readonly DependencyProperty GlobalCorpseDecompositionTimeMultiplierProperty = DependencyProperty.Register(nameof(GlobalCorpseDecompositionTimeMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float GlobalItemDecompositionTimeMultiplier
        {
            get { return (float)GetValue(GlobalItemDecompositionTimeMultiplierProperty); }
            set { SetValue(GlobalItemDecompositionTimeMultiplierProperty, value); }
        }

        public static readonly DependencyProperty GlobalItemDecompositionTimeMultiplierProperty = DependencyProperty.Register(nameof(GlobalItemDecompositionTimeMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float GlobalCorpseDecompositionTimeMultiplier
        {
            get { return (float)GetValue(GlobalCorpseDecompositionTimeMultiplierProperty); }
            set { SetValue(GlobalCorpseDecompositionTimeMultiplierProperty, value); }
        }

        public static readonly DependencyProperty CropDecaySpeedMultiplierProperty = DependencyProperty.Register(nameof(CropDecaySpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float CropDecaySpeedMultiplier
        {
            get { return (float)GetValue(CropDecaySpeedMultiplierProperty); }
            set { SetValue(CropDecaySpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty CropGrowthSpeedMultiplierProperty = DependencyProperty.Register(nameof(CropGrowthSpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float CropGrowthSpeedMultiplier
        {
            get { return (float)GetValue(CropGrowthSpeedMultiplierProperty); }
            set { SetValue(CropGrowthSpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty LayEggIntervalMultiplierProperty = DependencyProperty.Register(nameof(LayEggIntervalMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float LayEggIntervalMultiplier
        {
            get { return (float)GetValue(LayEggIntervalMultiplierProperty); }
            set { SetValue(LayEggIntervalMultiplierProperty, value); }
        }

        public static readonly DependencyProperty PoopIntervalMultiplierProperty = DependencyProperty.Register(nameof(PoopIntervalMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float PoopIntervalMultiplier
        {
            get { return (float)GetValue(PoopIntervalMultiplierProperty); }
            set { SetValue(PoopIntervalMultiplierProperty, value); }
        }

        public static readonly DependencyProperty HairGrowthSpeedMultiplierProperty = DependencyProperty.Register(nameof(HairGrowthSpeedMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float HairGrowthSpeedMultiplier
        {
            get { return (float)GetValue(HairGrowthSpeedMultiplierProperty); }
            set { SetValue(HairGrowthSpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty CraftXPMultiplierProperty = DependencyProperty.Register(nameof(CraftXPMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float CraftXPMultiplier
        {
            get { return (float)GetValue(CraftXPMultiplierProperty); }
            set { SetValue(CraftXPMultiplierProperty, value); }
        }

        public static readonly DependencyProperty GenericXPMultiplierProperty = DependencyProperty.Register(nameof(GenericXPMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float GenericXPMultiplier
        {
            get { return (float)GetValue(GenericXPMultiplierProperty); }
            set { SetValue(GenericXPMultiplierProperty, value); }
        }

        public static readonly DependencyProperty HarvestXPMultiplierProperty = DependencyProperty.Register(nameof(HarvestXPMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float HarvestXPMultiplier
        {
            get { return (float)GetValue(HarvestXPMultiplierProperty); }
            set { SetValue(HarvestXPMultiplierProperty, value); }
        }

        public static readonly DependencyProperty KillXPMultiplierProperty = DependencyProperty.Register(nameof(KillXPMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float KillXPMultiplier
        {
            get { return (float)GetValue(KillXPMultiplierProperty); }
            set { SetValue(KillXPMultiplierProperty, value); }
        }

        public static readonly DependencyProperty SpecialXPMultiplierProperty = DependencyProperty.Register(nameof(SpecialXPMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment, WriteIfNotValue = 1.0f)]
        public float SpecialXPMultiplier
        {
            get { return (float)GetValue(SpecialXPMultiplierProperty); }
            set { SetValue(SpecialXPMultiplierProperty, value); }
        }

        public static readonly DependencyProperty HarvestResourceItemAmountClassMultipliersProperty = DependencyProperty.Register(nameof(HarvestResourceItemAmountClassMultipliers), typeof(ResourceClassMultiplierList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Environment)]
        public ResourceClassMultiplierList HarvestResourceItemAmountClassMultipliers
        {
            get { return (ResourceClassMultiplierList)GetValue(HarvestResourceItemAmountClassMultipliersProperty); }
            set { SetValue(HarvestResourceItemAmountClassMultipliersProperty, value); }
        }
        #endregion

        #region Structures
        public static readonly DependencyProperty StructureResistanceMultiplierProperty = DependencyProperty.Register(nameof(StructureResistanceMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, WriteIfNotValue = 1.0f)]
        public float StructureResistanceMultiplier
        {
            get { return (float)GetValue(StructureResistanceMultiplierProperty); }
            set { SetValue(StructureResistanceMultiplierProperty, value); }
        }

        public static readonly DependencyProperty StructureDamageMultiplierProperty = DependencyProperty.Register(nameof(StructureDamageMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, WriteIfNotValue = 1.0f)]
        public float StructureDamageMultiplier
        {
            get { return (float)GetValue(StructureDamageMultiplierProperty); }
            set { SetValue(StructureDamageMultiplierProperty, value); }
        }

        public static readonly DependencyProperty StructureDamageRepairCooldownProperty = DependencyProperty.Register(nameof(StructureDamageRepairCooldown), typeof(int), typeof(ServerProfile), new PropertyMetadata(180));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures)]
        public int StructureDamageRepairCooldown
        {
            get { return (int)GetValue(StructureDamageRepairCooldownProperty); }
            set { SetValue(StructureDamageRepairCooldownProperty, value); }
        }

        public static readonly DependencyProperty PvPStructureDecayProperty = DependencyProperty.Register(nameof(PvPStructureDecay), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public bool PvPStructureDecay
        {
            get { return (bool)GetValue(PvPStructureDecayProperty); }
            set { SetValue(PvPStructureDecayProperty, value); }
        }

        public static readonly DependencyProperty PvPZoneStructureDamageMultiplierProperty = DependencyProperty.Register(nameof(PvPZoneStructureDamageMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(6.0f));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures)]
        public float PvPZoneStructureDamageMultiplier
        {
            get { return (float)GetValue(PvPZoneStructureDamageMultiplierProperty); }
            set { SetValue(PvPZoneStructureDamageMultiplierProperty, value); }
        }

        public static readonly DependencyProperty MaxStructuresInRangeProperty = DependencyProperty.Register(nameof(MaxStructuresInRange), typeof(int), typeof(ServerProfile), new PropertyMetadata(10500));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, "TheMaxStructuresInRange")]
        public int MaxStructuresInRange
        {
            get { return (int)GetValue(MaxStructuresInRangeProperty); }
            set { SetValue(MaxStructuresInRangeProperty, value); }
        }

        public static readonly DependencyProperty PerPlatformMaxStructuresMultiplierProperty = DependencyProperty.Register(nameof(PerPlatformMaxStructuresMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, WriteIfNotValue = 1.0f)]
        public float PerPlatformMaxStructuresMultiplier
        {
            get { return (float)GetValue(PerPlatformMaxStructuresMultiplierProperty); }
            set { SetValue(PerPlatformMaxStructuresMultiplierProperty, value); }
        }

        public static readonly DependencyProperty MaxPlatformSaddleStructureLimitProperty = DependencyProperty.Register(nameof(MaxPlatformSaddleStructureLimit), typeof(int), typeof(ServerProfile), new PropertyMetadata(0));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, WriteIfNotValue = 0)]
        public int MaxPlatformSaddleStructureLimit
        {
            get { return (int)GetValue(MaxPlatformSaddleStructureLimitProperty); }
            set { SetValue(MaxPlatformSaddleStructureLimitProperty, value); }
        }

        public static readonly DependencyProperty OverrideStructurePlatformPreventionProperty = DependencyProperty.Register(nameof(OverrideStructurePlatformPrevention), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, ConditionedOn = nameof(OverrideStructurePlatformPrevention))]
        public bool OverrideStructurePlatformPrevention
        {
            get { return (bool)GetValue(OverrideStructurePlatformPreventionProperty); }
            set { SetValue(OverrideStructurePlatformPreventionProperty, value); }
        }

        public static readonly DependencyProperty FlyerPlatformAllowUnalignedDinoBasingProperty = DependencyProperty.Register(nameof(FlyerPlatformAllowUnalignedDinoBasing), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, "bFlyerPlatformAllowUnalignedDinoBasing", ConditionedOn = nameof(FlyerPlatformAllowUnalignedDinoBasing))]
        public bool FlyerPlatformAllowUnalignedDinoBasing
        {
            get { return (bool)GetValue(FlyerPlatformAllowUnalignedDinoBasingProperty); }
            set { SetValue(FlyerPlatformAllowUnalignedDinoBasingProperty, value); }
        }

        public static readonly DependencyProperty PvEAllowStructuresAtSupplyDropsProperty = DependencyProperty.Register(nameof(PvEAllowStructuresAtSupplyDrops), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, ConditionedOn = nameof(PvEAllowStructuresAtSupplyDrops))]
        public bool PvEAllowStructuresAtSupplyDrops
        {
            get { return (bool)GetValue(PvEAllowStructuresAtSupplyDropsProperty); }
            set { SetValue(PvEAllowStructuresAtSupplyDropsProperty, value); }
        }

        public static readonly DependencyProperty EnableStructureDecayPvEProperty = DependencyProperty.Register(nameof(EnableStructureDecayPvE), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, "DisableStructureDecayPVE", InvertBoolean = true)]
        public bool EnableStructureDecayPvE
        {
            get { return (bool)GetValue(EnableStructureDecayPvEProperty); }
            set { SetValue(EnableStructureDecayPvEProperty, value); }
        }

        public static readonly DependencyProperty PvEStructureDecayPeriodMultiplierProperty = DependencyProperty.Register(nameof(PvEStructureDecayPeriodMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, ConditionedOn = nameof(EnableStructureDecayPvE))]
        public float PvEStructureDecayPeriodMultiplier
        {
            get { return (float)GetValue(PvEStructureDecayPeriodMultiplierProperty); }
            set { SetValue(PvEStructureDecayPeriodMultiplierProperty, value); }
        }

        public static readonly DependencyProperty AutoDestroyOldStructuresMultiplierProperty = DependencyProperty.Register(nameof(AutoDestroyOldStructuresMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(0.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures, WriteIfNotValue = 0.0f)]
        public float AutoDestroyOldStructuresMultiplier
        {
            get { return (float)GetValue(AutoDestroyOldStructuresMultiplierProperty); }
            set { SetValue(AutoDestroyOldStructuresMultiplierProperty, value); }
        }

        public static readonly DependencyProperty ForceAllStructureLockingProperty = DependencyProperty.Register(nameof(ForceAllStructureLocking), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public bool ForceAllStructureLocking
        {
            get { return (bool)GetValue(ForceAllStructureLockingProperty); }
            set { SetValue(ForceAllStructureLockingProperty, value); }
        }

        public static readonly DependencyProperty PassiveDefensesDamageRiderlessDinosProperty = DependencyProperty.Register(nameof(PassiveDefensesDamageRiderlessDinos), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, "bPassiveDefensesDamageRiderlessDinos")]
        public bool PassiveDefensesDamageRiderlessDinos
        {
            get { return (bool)GetValue(PassiveDefensesDamageRiderlessDinosProperty); }
            set { SetValue(PassiveDefensesDamageRiderlessDinosProperty, value); }
        }

        public static readonly DependencyProperty EnableAutoDestroyStructuresProperty = DependencyProperty.Register(nameof(EnableAutoDestroyStructures), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableAutoDestroyStructures
        {
            get { return (bool)GetValue(EnableAutoDestroyStructuresProperty); }
            set { SetValue(EnableAutoDestroyStructuresProperty, value); }
        }

        public static readonly DependencyProperty OnlyAutoDestroyCoreStructuresProperty = DependencyProperty.Register(nameof(OnlyAutoDestroyCoreStructures), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public bool OnlyAutoDestroyCoreStructures
        {
            get { return (bool)GetValue(OnlyAutoDestroyCoreStructuresProperty); }
            set { SetValue(OnlyAutoDestroyCoreStructuresProperty, value); }
        }

        public static readonly DependencyProperty OnlyDecayUnsnappedCoreStructuresProperty = DependencyProperty.Register(nameof(OnlyDecayUnsnappedCoreStructures), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public bool OnlyDecayUnsnappedCoreStructures
        {
            get { return (bool)GetValue(OnlyDecayUnsnappedCoreStructuresProperty); }
            set { SetValue(OnlyDecayUnsnappedCoreStructuresProperty, value); }
        }

        public static readonly DependencyProperty FastDecayUnsnappedCoreStructuresProperty = DependencyProperty.Register(nameof(FastDecayUnsnappedCoreStructures), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public bool FastDecayUnsnappedCoreStructures
        {
            get { return (bool)GetValue(FastDecayUnsnappedCoreStructuresProperty); }
            set { SetValue(FastDecayUnsnappedCoreStructuresProperty, value); }
        }

        public static readonly DependencyProperty DestroyUnconnectedWaterPipesProperty = DependencyProperty.Register(nameof(DestroyUnconnectedWaterPipes), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public bool DestroyUnconnectedWaterPipes
        {
            get { return (bool)GetValue(DestroyUnconnectedWaterPipesProperty); }
            set { SetValue(DestroyUnconnectedWaterPipesProperty, value); }
        }

        public static readonly DependencyProperty DisableStructurePlacementCollisionProperty = DependencyProperty.Register(nameof(DisableStructurePlacementCollision), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, "bDisableStructurePlacementCollision")]
        public bool DisableStructurePlacementCollision
        {
            get { return (bool)GetValue(DisableStructurePlacementCollisionProperty); }
            set { SetValue(DisableStructurePlacementCollisionProperty, value); }
        }

        public static readonly DependencyProperty IgnoreLimitMaxStructuresInRangeTypeFlagProperty = DependencyProperty.Register(nameof(IgnoreLimitMaxStructuresInRangeTypeFlag), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public bool IgnoreLimitMaxStructuresInRangeTypeFlag
        {
            get { return (bool)GetValue(IgnoreLimitMaxStructuresInRangeTypeFlagProperty); }
            set { SetValue(IgnoreLimitMaxStructuresInRangeTypeFlagProperty, value); }
        }

        public static readonly DependencyProperty EnableFastDecayIntervalProperty = DependencyProperty.Register(nameof(EnableFastDecayInterval), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool EnableFastDecayInterval
        {
            get { return (bool)GetValue(EnableFastDecayIntervalProperty); }
            set { SetValue(EnableFastDecayIntervalProperty, value); }
        }

        public static readonly DependencyProperty FastDecayIntervalProperty = DependencyProperty.Register(nameof(FastDecayInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(43200));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, ConditionedOn = nameof(EnableFastDecayInterval))]
        public int FastDecayInterval
        {
            get { return (int)GetValue(FastDecayIntervalProperty); }
            set { SetValue(FastDecayIntervalProperty, value); }
        }

        public static readonly DependencyProperty LimitTurretsInRangeProperty = DependencyProperty.Register(nameof(LimitTurretsInRange), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, "bLimitTurretsInRange")]
        public bool LimitTurretsInRange
        {
            get { return (bool)GetValue(LimitTurretsInRangeProperty); }
            set { SetValue(LimitTurretsInRangeProperty, value); }
        }

        public static readonly DependencyProperty LimitTurretsRangeProperty = DependencyProperty.Register(nameof(LimitTurretsRange), typeof(int), typeof(ServerProfile), new PropertyMetadata(10000));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, ConditionedOn = nameof(LimitTurretsInRange))]
        public int LimitTurretsRange
        {
            get { return (int)GetValue(LimitTurretsRangeProperty); }
            set { SetValue(LimitTurretsRangeProperty, value); }
        }

        public static readonly DependencyProperty LimitTurretsNumProperty = DependencyProperty.Register(nameof(LimitTurretsNum), typeof(int), typeof(ServerProfile), new PropertyMetadata(100));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, ConditionedOn = nameof(LimitTurretsInRange))]
        public int LimitTurretsNum
        {
            get { return (int)GetValue(LimitTurretsNumProperty); }
            set { SetValue(LimitTurretsNumProperty, value); }
        }

        public static readonly DependencyProperty HardLimitTurretsInRangeProperty = DependencyProperty.Register(nameof(HardLimitTurretsInRange), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, "bHardLimitTurretsInRange")]
        public bool HardLimitTurretsInRange
        {
            get { return (bool)GetValue(HardLimitTurretsInRangeProperty); }
            set { SetValue(HardLimitTurretsInRangeProperty, value); }
        }

        public static readonly DependencyProperty AlwaysAllowStructurePickupProperty = DependencyProperty.Register(nameof(AlwaysAllowStructurePickup), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public bool AlwaysAllowStructurePickup
        {
            get { return (bool)GetValue(AlwaysAllowStructurePickupProperty); }
            set { SetValue(AlwaysAllowStructurePickupProperty, value); }
        }

        public static readonly DependencyProperty StructurePickupTimeAfterPlacementProperty = DependencyProperty.Register(nameof(StructurePickupTimeAfterPlacement), typeof(float), typeof(ServerProfile), new PropertyMetadata(30.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public float StructurePickupTimeAfterPlacement
        {
            get { return (float)GetValue(StructurePickupTimeAfterPlacementProperty); }
            set { SetValue(StructurePickupTimeAfterPlacementProperty, value); }
        }

        public static readonly DependencyProperty StructurePickupHoldDurationProperty = DependencyProperty.Register(nameof(StructurePickupHoldDuration), typeof(float), typeof(ServerProfile), new PropertyMetadata(0.5f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public float StructurePickupHoldDuration
        {
            get { return (float)GetValue(StructurePickupHoldDurationProperty); }
            set { SetValue(StructurePickupHoldDurationProperty, value); }
        }

        public static readonly DependencyProperty AllowIntegratedSPlusStructuresProperty = DependencyProperty.Register(nameof(AllowIntegratedSPlusStructures), typeof(bool), typeof(ServerProfile), new PropertyMetadata(true));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.Structures)]
        public bool AllowIntegratedSPlusStructures
        {
            get { return (bool)GetValue(AllowIntegratedSPlusStructuresProperty); }
            set { SetValue(AllowIntegratedSPlusStructuresProperty, value); }
        }

        public static readonly DependencyProperty IgnoreStructuresPreventionVolumesProperty = DependencyProperty.Register(nameof(IgnoreStructuresPreventionVolumes), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, "bIgnoreStructuresPreventionVolumes")]
        public bool IgnoreStructuresPreventionVolumes
        {
            get { return (bool)GetValue(IgnoreStructuresPreventionVolumesProperty); }
            set { SetValue(IgnoreStructuresPreventionVolumesProperty, value); }
        }

        public static readonly DependencyProperty GenesisUseStructuresPreventionVolumesProperty = DependencyProperty.Register(nameof(GenesisUseStructuresPreventionVolumes), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Structures, "bGenesisUseStructuresPreventionVolumes")]
        public bool GenesisUseStructuresPreventionVolumes
        {
            get { return (bool)GetValue(GenesisUseStructuresPreventionVolumesProperty); }
            set { SetValue(GenesisUseStructuresPreventionVolumesProperty, value); }
        }
        #endregion

        #region Engrams
        public static readonly DependencyProperty AutoUnlockAllEngramsProperty = DependencyProperty.Register(nameof(AutoUnlockAllEngrams), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Engrams, "bAutoUnlockAllEngrams", ConditionedOn = nameof(AutoUnlockAllEngrams))]
        public bool AutoUnlockAllEngrams
        {
            get { return (bool)GetValue(AutoUnlockAllEngramsProperty); }
            set { SetValue(AutoUnlockAllEngramsProperty, value); }
        }

        public static readonly DependencyProperty OnlyAllowSpecifiedEngramsProperty = DependencyProperty.Register(nameof(OnlyAllowSpecifiedEngrams), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Engrams, "bOnlyAllowSpecifiedEngrams", ConditionedOn = nameof(OnlyAllowSpecifiedEngrams))]
        public bool OnlyAllowSpecifiedEngrams
        {
            get { return (bool)GetValue(OnlyAllowSpecifiedEngramsProperty); }
            set { SetValue(OnlyAllowSpecifiedEngramsProperty, value); }
        }

        public static readonly DependencyProperty OverrideNamedEngramEntriesProperty = DependencyProperty.Register(nameof(OverrideNamedEngramEntries), typeof(EngramEntryList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Engrams)]
        public EngramEntryList OverrideNamedEngramEntries
        {
            get { return (EngramEntryList)GetValue(OverrideNamedEngramEntriesProperty); }
            set { SetValue(OverrideNamedEngramEntriesProperty, value); }
        }

        public static readonly DependencyProperty EngramEntryAutoUnlocksProperty = DependencyProperty.Register(nameof(EngramEntryAutoUnlocks), typeof(EngramAutoUnlockList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.Engrams)]
        public EngramAutoUnlockList EngramEntryAutoUnlocks
        {
            get { return (EngramAutoUnlockList)GetValue(EngramEntryAutoUnlocksProperty); }
            set { SetValue(EngramEntryAutoUnlocksProperty, value); }
        }

        public static readonly DependencyProperty EngramSettingsProperty = DependencyProperty.Register(nameof(EngramSettings), typeof(EngramSettingsList), typeof(ServerProfile), new PropertyMetadata(null));
        public EngramSettingsList EngramSettings
        {
            get { return (EngramSettingsList)GetValue(EngramSettingsProperty); }
            set { SetValue(EngramSettingsProperty, value); }
        }
        #endregion

        #region Crafting Overrides
        public static readonly DependencyProperty ConfigOverrideItemCraftingCostsProperty = DependencyProperty.Register(nameof(ConfigOverrideItemCraftingCosts), typeof(CraftingOverrideList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.CraftingOverrides)]
        public CraftingOverrideList ConfigOverrideItemCraftingCosts
        {
            get { return (CraftingOverrideList)GetValue(ConfigOverrideItemCraftingCostsProperty); }
            set { SetValue(ConfigOverrideItemCraftingCostsProperty, value); }
        }
        #endregion

        #region Custom Levels
        public static readonly DependencyProperty OverrideMaxExperiencePointsPlayerProperty = DependencyProperty.Register(nameof(OverrideMaxExperiencePointsPlayer), typeof(NullableValue<long>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<long>(false, GameData.DefaultMaxExperiencePointsPlayer)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.CustomLevels)]
        public NullableValue<long> OverrideMaxExperiencePointsPlayer
        {
            get { return (NullableValue<long>)GetValue(OverrideMaxExperiencePointsPlayerProperty); }
            set { SetValue(OverrideMaxExperiencePointsPlayerProperty, value); }
        }

        public static readonly DependencyProperty OverrideMaxExperiencePointsDinoProperty = DependencyProperty.Register(nameof(OverrideMaxExperiencePointsDino), typeof(NullableValue<long>), typeof(ServerProfile), new PropertyMetadata(new NullableValue<long>(false, GameData.DefaultMaxExperiencePointsDino)));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.CustomLevels)]
        public NullableValue<long> OverrideMaxExperiencePointsDino
        {
            get { return (NullableValue<long>)GetValue(OverrideMaxExperiencePointsDinoProperty); }
            set { SetValue(OverrideMaxExperiencePointsDinoProperty, value); }
        }

        public static readonly DependencyProperty EnableLevelProgressionsProperty = DependencyProperty.Register(nameof(EnableLevelProgressions), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool EnableLevelProgressions
        {
            get { return (bool)GetValue(EnableLevelProgressionsProperty); }
            set { SetValue(EnableLevelProgressionsProperty, value); }
        }

        public static readonly DependencyProperty PlayerLevelsProperty = DependencyProperty.Register(nameof(PlayerLevels), typeof(LevelList), typeof(ServerProfile), new PropertyMetadata());
        public LevelList PlayerLevels
        {
            get { return (LevelList)GetValue(PlayerLevelsProperty); }
            set { SetValue(PlayerLevelsProperty, value); }
        }

        public static readonly DependencyProperty EnableDinoLevelProgressionsProperty = DependencyProperty.Register(nameof(EnableDinoLevelProgressions), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        public bool EnableDinoLevelProgressions
        {
            get { return (bool)GetValue(EnableDinoLevelProgressionsProperty); }
            set { SetValue(EnableDinoLevelProgressionsProperty, value); }
        }

        public static readonly DependencyProperty DinoLevelsProperty = DependencyProperty.Register(nameof(DinoLevels), typeof(LevelList), typeof(ServerProfile), new PropertyMetadata());
        public LevelList DinoLevels
        {
            get { return (LevelList)GetValue(DinoLevelsProperty); }
            set { SetValue(DinoLevelsProperty, value); }
        }
        #endregion

        #region Custom Settings
        public static readonly DependencyProperty CustomGameUserSettingsProperty = DependencyProperty.Register(nameof(CustomGameUserSettings), typeof(CustomList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.Custom, ServerProfileCategory.CustomGameUserSettings, IsCustom = true)]
        public CustomList CustomGameUserSettings
        {
            get { return (CustomList)GetValue(CustomGameUserSettingsProperty); }
            set { SetValue(CustomGameUserSettingsProperty, value); }
        }

        public static readonly DependencyProperty CustomGameSettingsProperty = DependencyProperty.Register(nameof(CustomGameSettings), typeof(CustomList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Custom, ServerProfileCategory.CustomGameSettings, IsCustom = true)]
        public CustomList CustomGameSettings
        {
            get { return (CustomList)GetValue(CustomGameSettingsProperty); }
            set { SetValue(CustomGameSettingsProperty, value); }
        }

        public static readonly DependencyProperty CustomEngineSettingsProperty = DependencyProperty.Register(nameof(CustomEngineSettings), typeof(CustomList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Engine, IniSections.Custom, ServerProfileCategory.CustomEngineSettings, IsCustom = true)]
        public CustomList CustomEngineSettings
        {
            get { return (CustomList)GetValue(CustomEngineSettingsProperty); }
            set { SetValue(CustomEngineSettingsProperty, value); }
        }
        #endregion

        #region Server Files
        public static readonly DependencyProperty ServerFilesAdminsProperty = DependencyProperty.Register(nameof(ServerFilesAdmins), typeof(PlayerUserList), typeof(ServerProfile), new PropertyMetadata(null));
        [DataMember]
        public PlayerUserList ServerFilesAdmins
        {
            get { return (PlayerUserList)GetValue(ServerFilesAdminsProperty); }
            set { SetValue(ServerFilesAdminsProperty, value); }
        }

        public static readonly DependencyProperty EnableExclusiveJoinProperty = DependencyProperty.Register(nameof(EnableExclusiveJoin), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool EnableExclusiveJoin
        {
            get { return (bool)GetValue(EnableExclusiveJoinProperty); }
            set { SetValue(EnableExclusiveJoinProperty, value); }
        }

        public static readonly DependencyProperty ServerFilesExclusiveProperty = DependencyProperty.Register(nameof(ServerFilesExclusive), typeof(PlayerUserList), typeof(ServerProfile), new PropertyMetadata(null));
        [DataMember]
        public PlayerUserList ServerFilesExclusive
        {
            get { return (PlayerUserList)GetValue(ServerFilesExclusiveProperty); }
            set { SetValue(ServerFilesExclusiveProperty, value); }
        }

        public static readonly DependencyProperty ServerFilesWhitelistedProperty = DependencyProperty.Register(nameof(ServerFilesWhitelisted), typeof(PlayerUserList), typeof(ServerProfile), new PropertyMetadata(null));
        [DataMember]
        public PlayerUserList ServerFilesWhitelisted
        {
            get { return (PlayerUserList)GetValue(ServerFilesWhitelistedProperty); }
            set { SetValue(ServerFilesWhitelistedProperty, value); }
        }
        #endregion

        #region Procedurally Generated ARKS
        public static readonly DependencyProperty PGM_EnabledProperty = DependencyProperty.Register(nameof(PGM_Enabled), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool PGM_Enabled
        {
            get { return (bool)GetValue(PGM_EnabledProperty); }
            set { SetValue(PGM_EnabledProperty, value); }
        }

        public static readonly DependencyProperty PGM_NameProperty = DependencyProperty.Register(nameof(PGM_Name), typeof(string), typeof(ServerProfile), new PropertyMetadata(Config.Default.DefaultPGMapName));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.PGM, "PGMapName", ConditionedOn = nameof(PGM_Enabled))]
        public string PGM_Name
        {
            get { return (string)GetValue(PGM_NameProperty); }
            set { SetValue(PGM_NameProperty, value); }
        }

        public static readonly DependencyProperty PGM_TerrainProperty = DependencyProperty.Register(nameof(PGM_Terrain), typeof(PGMTerrain), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.PGM, "PGTerrainPropertiesString", ConditionedOn = nameof(PGM_Enabled))]
        public PGMTerrain PGM_Terrain
        {
            get { return (PGMTerrain)GetValue(PGM_TerrainProperty); }
            set { SetValue(PGM_TerrainProperty, value); }
        }
        #endregion

        #region NPC Spawn Overrides
        public static readonly DependencyProperty ConfigAddNPCSpawnEntriesContainerProperty = DependencyProperty.Register(nameof(ConfigAddNPCSpawnEntriesContainer), typeof(NPCSpawnContainerList<NPCSpawnContainer>), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.MapSpawnerOverrides)]
        public NPCSpawnContainerList<NPCSpawnContainer> ConfigAddNPCSpawnEntriesContainer
        {
            get { return (NPCSpawnContainerList<NPCSpawnContainer>)GetValue(ConfigAddNPCSpawnEntriesContainerProperty); }
            set { SetValue(ConfigAddNPCSpawnEntriesContainerProperty, value); }
        }

        public static readonly DependencyProperty ConfigSubtractNPCSpawnEntriesContainerProperty = DependencyProperty.Register(nameof(ConfigSubtractNPCSpawnEntriesContainer), typeof(NPCSpawnContainerList<NPCSpawnContainer>), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.MapSpawnerOverrides)]
        public NPCSpawnContainerList<NPCSpawnContainer> ConfigSubtractNPCSpawnEntriesContainer
        {
            get { return (NPCSpawnContainerList<NPCSpawnContainer>)GetValue(ConfigSubtractNPCSpawnEntriesContainerProperty); }
            set { SetValue(ConfigSubtractNPCSpawnEntriesContainerProperty, value); }
        }

        public static readonly DependencyProperty ConfigOverrideNPCSpawnEntriesContainerProperty = DependencyProperty.Register(nameof(ConfigOverrideNPCSpawnEntriesContainer), typeof(NPCSpawnContainerList<NPCSpawnContainer>), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.MapSpawnerOverrides)]
        public NPCSpawnContainerList<NPCSpawnContainer> ConfigOverrideNPCSpawnEntriesContainer
        {
            get { return (NPCSpawnContainerList<NPCSpawnContainer>)GetValue(ConfigOverrideNPCSpawnEntriesContainerProperty); }
            set { SetValue(ConfigOverrideNPCSpawnEntriesContainerProperty, value); }
        }

        public static readonly DependencyProperty NPCSpawnSettingsProperty = DependencyProperty.Register(nameof(NPCSpawnSettings), typeof(NPCSpawnSettingsList), typeof(ServerProfile), new PropertyMetadata(null));
        public NPCSpawnSettingsList NPCSpawnSettings
        {
            get { return (NPCSpawnSettingsList)GetValue(NPCSpawnSettingsProperty); }
            set { SetValue(NPCSpawnSettingsProperty, value); }
        }
        #endregion

        #region Supply Crate Overrides
        public static readonly DependencyProperty ConfigOverrideSupplyCrateItemsProperty = DependencyProperty.Register(nameof(ConfigOverrideSupplyCrateItems), typeof(SupplyCrateOverrideList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.SupplyCrateOverrides)]
        public SupplyCrateOverrideList ConfigOverrideSupplyCrateItems
        {
            get { return (SupplyCrateOverrideList)GetValue(ConfigOverrideSupplyCrateItemsProperty); }
            set { SetValue(ConfigOverrideSupplyCrateItemsProperty, value); }
        }
        #endregion

        #region Exclude Item Indices Overrides
        public static readonly DependencyProperty ExcludeItemIndicesProperty = DependencyProperty.Register(nameof(ExcludeItemIndices), typeof(ExcludeItemIndicesOverrideList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.ExcludeItemIndicesOverrides)]
        public ExcludeItemIndicesOverrideList ExcludeItemIndices
        {
            get { return (ExcludeItemIndicesOverrideList)GetValue(ExcludeItemIndicesProperty); }
            set { SetValue(ExcludeItemIndicesProperty, value); }
        }
        #endregion

        #region Stacking Overrides 
        public static readonly DependencyProperty ItemStackSizeMultiplierProperty = DependencyProperty.Register(nameof(ItemStackSizeMultiplier), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.StackSizeOverrides, WriteIfNotValue = 1.0f)]
        public float ItemStackSizeMultiplier
        {
            get { return (float)GetValue(ItemStackSizeMultiplierProperty); }
            set { SetValue(ItemStackSizeMultiplierProperty, value); }
        }

        public static readonly DependencyProperty ConfigOverrideItemMaxQuantityProperty = DependencyProperty.Register(nameof(ConfigOverrideItemMaxQuantity), typeof(StackSizeOverrideList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.StackSizeOverrides)]
        public StackSizeOverrideList ConfigOverrideItemMaxQuantity
        {
            get { return (StackSizeOverrideList)GetValue(ConfigOverrideItemMaxQuantityProperty); }
            set { SetValue(ConfigOverrideItemMaxQuantityProperty, value); }
        }
        #endregion

        #region Prevent Transfer Overrides 
        public static readonly DependencyProperty PreventTransferForClassNamesProperty = DependencyProperty.Register(nameof(PreventTransferForClassNames), typeof(PreventTransferOverrideList), typeof(ServerProfile), new PropertyMetadata(null));
        [IniFileEntry(IniFiles.Game, IniSections.Game_ShooterGameMode, ServerProfileCategory.PreventTransferOverrides)]
        public PreventTransferOverrideList PreventTransferForClassNames
        {
            get { return (PreventTransferOverrideList)GetValue(PreventTransferForClassNamesProperty); }
            set { SetValue(PreventTransferForClassNamesProperty, value); }
        }
        #endregion

        #region Survival of the Fittest
        public static readonly DependencyProperty SOTF_EnabledProperty = DependencyProperty.Register(nameof(SOTF_Enabled), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool SOTF_Enabled
        {
            get { return (bool)GetValue(SOTF_EnabledProperty); }
            set { SetValue(SOTF_EnabledProperty, value); }
        }

        public static readonly DependencyProperty SOTF_DisableDeathSPectatorProperty = DependencyProperty.Register(nameof(SOTF_DisableDeathSPectator), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool SOTF_DisableDeathSPectator
        {
            get { return (bool)GetValue(SOTF_DisableDeathSPectatorProperty); }
            set { SetValue(SOTF_DisableDeathSPectatorProperty, value); }
        }

        public static readonly DependencyProperty SOTF_OnlyAdminRejoinAsSpectatorProperty = DependencyProperty.Register(nameof(SOTF_OnlyAdminRejoinAsSpectator), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool SOTF_OnlyAdminRejoinAsSpectator
        {
            get { return (bool)GetValue(SOTF_OnlyAdminRejoinAsSpectatorProperty); }
            set { SetValue(SOTF_OnlyAdminRejoinAsSpectatorProperty, value); }
        }

        public static readonly DependencyProperty SOTF_GamePlayLoggingProperty = DependencyProperty.Register(nameof(SOTF_GamePlayLogging), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool SOTF_GamePlayLogging
        {
            get { return (bool)GetValue(SOTF_GamePlayLoggingProperty); }
            set { SetValue(SOTF_GamePlayLoggingProperty, value); }
        }

        public static readonly DependencyProperty SOTF_OutputGameReportProperty = DependencyProperty.Register(nameof(SOTF_OutputGameReport), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool SOTF_OutputGameReport
        {
            get { return (bool)GetValue(SOTF_OutputGameReportProperty); }
            set { SetValue(SOTF_OutputGameReportProperty, value); }
        }

        public static readonly DependencyProperty SOTF_MaxNumberOfPlayersInTribeProperty = DependencyProperty.Register(nameof(SOTF_MaxNumberOfPlayersInTribe), typeof(int), typeof(ServerProfile), new PropertyMetadata(2));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.SOTF, "MaxNumberOfPlayersInTribe", ConditionedOn = nameof(SOTF_Enabled))]
        public int SOTF_MaxNumberOfPlayersInTribe
        {
            get { return (int)GetValue(SOTF_MaxNumberOfPlayersInTribeProperty); }
            set { SetValue(SOTF_MaxNumberOfPlayersInTribeProperty, value); }
        }

        public static readonly DependencyProperty SOTF_BattleNumOfTribesToStartGameProperty = DependencyProperty.Register(nameof(SOTF_BattleNumOfTribesToStartGame), typeof(int), typeof(ServerProfile), new PropertyMetadata(15));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.SOTF, "BattleNumOfTribesToStartGame", ConditionedOn = nameof(SOTF_Enabled))]
        public int SOTF_BattleNumOfTribesToStartGame
        {
            get { return (int)GetValue(SOTF_BattleNumOfTribesToStartGameProperty); }
            set { SetValue(SOTF_BattleNumOfTribesToStartGameProperty, value); }
        }

        public static readonly DependencyProperty SOTF_TimeToCollapseRODProperty = DependencyProperty.Register(nameof(SOTF_TimeToCollapseROD), typeof(int), typeof(ServerProfile), new PropertyMetadata(9000));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.SOTF, "TimeToCollapseROD", ConditionedOn = nameof(SOTF_Enabled))]
        public int SOTF_TimeToCollapseROD
        {
            get { return (int)GetValue(SOTF_TimeToCollapseRODProperty); }
            set { SetValue(SOTF_TimeToCollapseRODProperty, value); }
        }

        public static readonly DependencyProperty SOTF_BattleAutoStartGameIntervalProperty = DependencyProperty.Register(nameof(SOTF_BattleAutoStartGameInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(60));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.SOTF, "BattleAutoStartGameInterval", ConditionedOn = nameof(SOTF_Enabled))]
        public int SOTF_BattleAutoStartGameInterval
        {
            get { return (int)GetValue(SOTF_BattleAutoStartGameIntervalProperty); }
            set { SetValue(SOTF_BattleAutoStartGameIntervalProperty, value); }
        }

        public static readonly DependencyProperty SOTF_BattleAutoRestartGameIntervalProperty = DependencyProperty.Register(nameof(SOTF_BattleAutoRestartGameInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(45));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.SOTF, "BattleAutoRestartGameInterval", ConditionedOn = nameof(SOTF_Enabled))]
        public int SOTF_BattleAutoRestartGameInterval
        {
            get { return (int)GetValue(SOTF_BattleAutoRestartGameIntervalProperty); }
            set { SetValue(SOTF_BattleAutoRestartGameIntervalProperty, value); }
        }

        public static readonly DependencyProperty SOTF_BattleSuddenDeathIntervalProperty = DependencyProperty.Register(nameof(SOTF_BattleSuddenDeathInterval), typeof(int), typeof(ServerProfile), new PropertyMetadata(300));
        [IniFileEntry(IniFiles.GameUserSettings, IniSections.GUS_ServerSettings, ServerProfileCategory.SOTF, "BattleSuddenDeathInterval", ConditionedOn = nameof(SOTF_Enabled))]
        public int SOTF_BattleSuddenDeathInterval
        {
            get { return (int)GetValue(SOTF_BattleSuddenDeathIntervalProperty); }
            set { SetValue(SOTF_BattleSuddenDeathIntervalProperty, value); }
        }

        public static readonly DependencyProperty SOTF_NoEventsProperty = DependencyProperty.Register(nameof(SOTF_NoEvents), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool SOTF_NoEvents
        {
            get { return (bool)GetValue(SOTF_NoEventsProperty); }
            set { SetValue(SOTF_NoEventsProperty, value); }
        }

        public static readonly DependencyProperty SOTF_NoBossesProperty = DependencyProperty.Register(nameof(SOTF_NoBosses), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool SOTF_NoBosses
        {
            get { return (bool)GetValue(SOTF_NoBossesProperty); }
            set { SetValue(SOTF_NoBossesProperty, value); }
        }

        public static readonly DependencyProperty SOTF_BothBossesProperty = DependencyProperty.Register(nameof(SOTF_BothBosses), typeof(bool), typeof(ServerProfile), new PropertyMetadata(false));
        [DataMember]
        public bool SOTF_BothBosses
        {
            get { return (bool)GetValue(SOTF_BothBossesProperty); }
            set { SetValue(SOTF_BothBossesProperty, value); }
        }

        public static readonly DependencyProperty SOTF_EvoEventIntervalProperty = DependencyProperty.Register(nameof(SOTF_EvoEventInterval), typeof(float), typeof(ServerProfile), new PropertyMetadata(1.0f));
        [DataMember]
        public float SOTF_EvoEventInterval
        {
            get { return (float)GetValue(SOTF_EvoEventIntervalProperty); }
            set { SetValue(SOTF_EvoEventIntervalProperty, value); }
        }

        public static readonly DependencyProperty SOTF_RingStartTimeProperty = DependencyProperty.Register(nameof(SOTF_RingStartTime), typeof(float), typeof(ServerProfile), new PropertyMetadata(1000.0f));
        [DataMember]
        public float SOTF_RingStartTime
        {
            get { return (float)GetValue(SOTF_RingStartTimeProperty); }
            set { SetValue(SOTF_RingStartTimeProperty, value); }
        }
        #endregion

        #region RCON
        public static readonly DependencyProperty RCONWindowExtentsProperty = DependencyProperty.Register(nameof(RCONWindowExtents), typeof(Rect), typeof(ServerProfile), new PropertyMetadata(new Rect(0f, 0f, 0f, 0f)));
        [DataMember]
        public Rect RCONWindowExtents
        {
            get { return (Rect)GetValue(RCONWindowExtentsProperty); }
            set { SetValue(RCONWindowExtentsProperty, value); }
        }

        public static readonly DependencyProperty RCONPlayerListWidthProperty = DependencyProperty.Register(nameof(RCONPlayerListWidth), typeof(double), typeof(ServerProfile), new PropertyMetadata(200d));
        [DataMember]
        public double RCONPlayerListWidth
        {
            get { return (double)GetValue(RCONPlayerListWidthProperty); }
            set { SetValue(RCONPlayerListWidthProperty, value); }
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
                    var serverConfigFile = Path.Combine(serverConfigPath, Config.Default.ServerGameUserSettingsConfigFile);
                    if (File.Exists(serverConfigFile))
                    {
                        LoadFromConfigFiles(serverConfigFile, this, exclusions: null);
                    }
                }
            }

            LoadServerFiles(true, true, true);
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
                var appIdServer = SOTF_Enabled ? Config.Default.AppIdServer_SotF : Config.Default.AppIdServer;
                var manifestFile = ModUtils.GetSteamManifestFile(InstallDirectory, appIdServer);
                if (string.IsNullOrWhiteSpace(manifestFile) || !File.Exists(manifestFile))
                    return;

                // load the manifest files
                var vdfDeserializer = VdfDeserializer.FromFile(manifestFile);
                var vdf = vdfDeserializer.Deserialize();

                // clear any of th beta keys values
                var updated = SteamCmdManifestDetailsResult.ClearUserConfigBetaKeys(vdf);

                if (updated)
                {
                    // save the manifest file
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
            settings.DinoSpawnWeightMultipliers.Reset();
            settings.TamedDinoClassResistanceMultipliers.Reset();
            settings.TamedDinoClassDamageMultipliers.Reset();
            settings.DinoClassResistanceMultipliers.Reset();
            settings.DinoClassDamageMultipliers.Reset();
            settings.HarvestResourceItemAmountClassMultipliers.Reset();
            settings.ResetLevelProgressionToOfficial(LevelProgression.Player);
            settings.ResetLevelProgressionToOfficial(LevelProgression.Dino);
            settings.PerLevelStatsMultiplier_DinoTamed.Reset();
            settings.PerLevelStatsMultiplier_DinoTamed_Add.Reset();
            settings.PerLevelStatsMultiplier_DinoTamed_Affinity.Reset();
            settings.PerLevelStatsMultiplier_DinoWild.Reset();
            settings.MutagenLevelBoost.Reset();
            settings.MutagenLevelBoost_Bred.Reset();
            settings.PerLevelStatsMultiplier_Player.Reset();
            settings.PlayerBaseStatMultipliers.Reset();
            settings.LoadServerFiles(true, true, true);
            settings.SetupServerFilesWatcher();
            return settings;
        }

        private void GetDefaultDirectories()
        {
            if (!string.IsNullOrWhiteSpace(InstallDirectory))
                return;

            // get the root servers folder
            var installDirectory = Path.IsPathRooted(Config.Default.ServersInstallDir)
                                       ? Path.Combine(Config.Default.ServersInstallDir)
                                       : Path.Combine(Config.Default.DataDir, Config.Default.ServersInstallDir);
            var index = 1;
            while (true)
            {
                // create a test profile folder name
                var profileFolder = $"{Config.Default.DefaultServerFolderName}{index}";
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
            var exclusions = new List<Enum>();

            if (!Config.Default.SectionCustomEngineSettingsEnabled)
            {
                exclusions.Add(ServerProfileCategory.CustomEngineSettings);
            }

            if (!Config.Default.SectionCraftingOverridesEnabled)
            {
                exclusions.Add(ServerProfileCategory.CraftingOverrides);
            }

            if (!Config.Default.SectionMapSpawnerOverridesEnabled)
            {
                exclusions.Add(ServerProfileCategory.MapSpawnerOverrides);
            }

            if (!Config.Default.SectionSupplyCrateOverridesEnabled)
            {
                exclusions.Add(ServerProfileCategory.SupplyCrateOverrides);
            }

            if (!Config.Default.SectionStackSizeOverridesEnabled)
            {
                exclusions.Add(ServerProfileCategory.StackSizeOverrides);
            }

            if (!Config.Default.SectionPreventTransferOverridesEnabled)
            {
                exclusions.Add(ServerProfileCategory.PreventTransferOverrides);
            }

            if (!Config.Default.SectionPGMEnabled)
            {
                exclusions.Add(ServerProfileCategory.PGM);
            }

            if (!Config.Default.SectionSOTFEnabled)
            {
                exclusions.Add(ServerProfileCategory.SOTF);
            }

            return exclusions;
        }

        private LevelList GetLevelList(LevelProgression levelProgression)
        {
            switch (levelProgression)
            {
                case LevelProgression.Player:
                    return this.PlayerLevels;

                case LevelProgression.Dino:
                    return this.DinoLevels;

                default:
                    return new LevelList();
            }
        }

        public string GetLauncherFile() => Path.Combine(GetProfileServerConfigDir(), Config.Default.LauncherFile);

        [Obsolete("This method will be removed in a future version.")]
        public string GetProfileConfigDir_Old() => Path.Combine(Path.GetDirectoryName(GetProfileFile()), this.ProfileName);

        public string GetProfileFile() => Path.Combine(Config.Default.ConfigDirectory, Path.ChangeExtension(this.ProfileID.ToLower(), Config.Default.ProfileExtension));

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

        public string GetServerExeFile() => Path.Combine(this.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe);

        public string GetServerArgs()
        {
            var serverArgs = new StringBuilder();

            serverArgs.Append(GetProfileMapName(this));

            serverArgs.Append("?listen");

            // These are used to match the server to the profile.
            if (!string.IsNullOrWhiteSpace(this.ServerIP))
            {
                serverArgs.Append("?MultiHome=").Append(this.ServerIP);
            }
            serverArgs.Append("?Port=").Append(this.ServerPort);
            serverArgs.Append("?QueryPort=").Append(this.QueryPort);
            serverArgs.Append("?MaxPlayers=").Append(this.MaxPlayers);

            if (!string.IsNullOrWhiteSpace(this.AltSaveDirectoryName))
            {
                serverArgs.Append($"?AltSaveDirectoryName={this.AltSaveDirectoryName}");
            }
            
            if (this.EnableServerAutoForceRespawnWildDinosInterval)
            {
                serverArgs.Append("?ServerAutoForceRespawnWildDinosInterval=").Append(this.ServerAutoForceRespawnWildDinosInterval);
            }

            serverArgs.Append($"?AllowCrateSpawnsOnTopOfStructures={this.AllowCrateSpawnsOnTopOfStructures}");

            if (this.ClampItemStats)
            {
                serverArgs.Append("?ClampItemStats=true");
            }

            if (Config.Default.SectionSOTFEnabled && this.SOTF_Enabled)
            {
                serverArgs.Append("?EvoEventInterval=").Append(this.SOTF_EvoEventInterval);
                serverArgs.Append("?RingStartTime=").Append(this.SOTF_RingStartTime);
            }

            if (this.EventColorsChanceOverride > 0)
            {
                serverArgs.Append("?EventColorsChanceOverride=").Append(this.EventColorsChanceOverride);
            }
            
            if (this.NewYear1UTC.HasValue)
            {
                serverArgs.Append("?NewYear1UTC=").Append((new DateTimeOffset(this.NewYear1UTC.Value.ToUniversalTime())).ToUnixTimeSeconds().ToString());
            }

            if (this.NewYear2UTC.HasValue)
            {
                serverArgs.Append("?NewYear2UTC=").Append((new DateTimeOffset(this.NewYear2UTC.Value.ToUniversalTime())).ToUnixTimeSeconds().ToString());
            }
            
            if (!string.IsNullOrWhiteSpace(this.AdditionalArgs))
            {
                var addArgs = this.AdditionalArgs.TrimStart();
                if (!addArgs.StartsWith("?"))
                    serverArgs.Append(" ");
                serverArgs.Append(addArgs);
            }

            if (!string.IsNullOrWhiteSpace(this.TotalConversionModId))
            {
                serverArgs.Append($" -TotalConversionMod={this.TotalConversionModId}");
            }

            if (!string.IsNullOrWhiteSpace(this.EventName))
            {
                serverArgs.Append($" -ActiveEvent={this.EventName}");
            }

            if (this.EnableAllowCaveFlyers)
            {
                serverArgs.Append(" -ForceAllowCaveFlyers");
            }

            if (this.EnableAutoDestroyStructures)
            {
                serverArgs.Append(" -AutoDestroyStructures");
            }

            if (this.KickIdlePlayersPeriod.HasValue)
            {
                serverArgs.Append(" -EnableIdlePlayerKick");
            }

            if (Config.Default.SectionSOTFEnabled && this.SOTF_Enabled)
            {
                if (this.SOTF_OutputGameReport)
                {
                    serverArgs.Append(" -OutputGameReport");
                }

                if (this.SOTF_GamePlayLogging)
                {
                    serverArgs.Append(" -gameplaylogging");
                }

                if (this.SOTF_DisableDeathSPectator)
                {
                    serverArgs.Append(" -DisableDeathSpectator");
                }

                if(this.SOTF_OnlyAdminRejoinAsSpectator)
                {
                    serverArgs.Append(" -OnlyAdminRejoinAsSpectator");
                }

                if (this.SOTF_NoEvents)
                {
                    serverArgs.Append(" -noevents");
                }

                if (this.SOTF_NoBosses)
                {
                    serverArgs.Append(" -nobosses");
                }
                else if (this.SOTF_BothBosses)
                {
                    serverArgs.Append(" -bothbosses");
                }
            }

            if (!string.IsNullOrWhiteSpace(this.CrossArkClusterId))
            {
                serverArgs.Append($" -clusterid={this.CrossArkClusterId}");

                if (this.ClusterDirOverride)
                {
                    serverArgs.Append($" -ClusterDirOverride=\"{Config.Default.DataDir}\"");
                }

                if (this.NoTransferFromFiltering)
                {
                    serverArgs.Append(" -NoTransferFromFiltering");
                }
            }

            if (this.DisableCustomFoldersInTributeInventories)
            {
                serverArgs.Append(" -DisableCustomFoldersInTributeInventories");
            }

            if (this.EnableWebAlarm)
            {
                serverArgs.Append(" -webalarm");
            }

            if (this.UseBattlEye)
            {
                serverArgs.Append(" -UseBattlEye");
            }

            if (!this.UseBattlEye)
            {
                serverArgs.Append(" -NoBattlEye");
            }

            if (this.DisableValveAntiCheatSystem)
            {
                serverArgs.Append(" -insecure");
            }

            if (this.DisableAntiSpeedHackDetection || this.SpeedHackBias == 0.0f)
            {
                serverArgs.Append(" -noantispeedhack");
            }
            else if (this.SpeedHackBias != 1.0f)
            {
                serverArgs.Append($" -speedhackbias={this.SpeedHackBias}f");
            }

            if (this.DisablePlayerMovePhysicsOptimization)
            {
                serverArgs.Append(" -nocombineclientmoves");
            }

            if (this.ForceRespawnDinos)
            {
                serverArgs.Append(" -forcerespawndinos");
            }

            if (this.EnableServerAdminLogs)
            {
                serverArgs.Append(" -servergamelog");
            }

            if (this.ServerAdminLogsIncludeTribeLogs)
            {
                serverArgs.Append(" -servergamelogincludetribelogs");
            }

            if (this.ServerRCONOutputTribeLogs)
            {
                serverArgs.Append(" -ServerRCONOutputTribeLogs");
            }

            if (this.NotifyAdminCommandsInChat)
            {
                serverArgs.Append(" -NotifyAdminCommandsInChat");
            }

            if (this.ForceDirectX10)
            {
                serverArgs.Append(" -d3d10");
            }

            if (this.ForceShaderModel4)
            {
                serverArgs.Append(" -sm4");
            }

            if (this.ForceLowMemory)
            {
                serverArgs.Append(" -lowmemory");
            }

            if (this.ForceNoManSky)
            {
                serverArgs.Append(" -nomansky");
            }

            if (this.UseNoMemoryBias)
            {
                serverArgs.Append(" -nomemorybias");
            }

            if (this.StasisKeepControllers)
            {
                serverArgs.Append(" -StasisKeepControllers");
            }

            if (this.UseNoHangDetection)
            {
                serverArgs.Append(" -NoHangDetection");
            }

            if (this.EnableExclusiveJoin)
            {
                serverArgs.Append(" -exclusivejoin");
            }

            if (this.ServerAllowAnsel)
            {
                serverArgs.Append(" -ServerAllowAnsel");
            }

            if (this.StructureMemoryOptimizations)
            {
                serverArgs.Append(" -structurememopts");
            }

            if (this.UseStructureStasisGrid)
            {
                serverArgs.Append(" -UseStructureStasisGrid");
            }

            if (this.SecureSendArKPayload)
            {
                serverArgs.Append(" -SecureSendArKPayload");
            }

            if (this.UseItemDupeCheck)
            {
                serverArgs.Append(" -UseItemDupeCheck");
            }

            if (this.UseSecureSpawnRules)
            {
                serverArgs.Append(" -UseSecureSpawnRules");
            }

            if (this.NoUnderMeshChecking)
            {
                serverArgs.Append(" -noundermeshchecking");
            }

            if (this.NoUnderMeshKilling)
            {
                serverArgs.Append(" -noundermeshkilling");
            }

            if (this.NoDinos)
            {
                serverArgs.Append(" -NoDinos");
            }

            if (this.Crossplay)
            {
                serverArgs.Append(" -crossplay");
            }

            if (this.EpicOnly)
            {
                serverArgs.Append(" -epiconly");
            }

            if (this.EnableCustomDynamicConfigUrl && !string.IsNullOrWhiteSpace(this.CustomDynamicConfigUrl))
            {
                serverArgs.Append(" -UseDynamicConfig ");
            }

            if ((this.Crossplay || this.EpicOnly) && this.EnablePublicIPForEpic)
            {
                serverArgs.Append($" -PublicIPForEpic={Config.Default.MachinePublicIP}");
            }

            if (this.DisableRailgunPVP)
            {
                serverArgs.Append(" -DisableRailgunPVP");
            }

            if (this.UseVivox)
            {
                serverArgs.Append(" -UseVivox");
            }

            serverArgs.Append(' ');
            serverArgs.Append(Config.Default.ServerCommandLineStandardArgs);

            if (this.OutputServerLog)
            {
                serverArgs.Append($" -log");
            }

            if (this.MinimumTimeBetweenInventoryRetrieval > 0)
            {
                serverArgs.Append(" -MinimumTimeBetweenInventoryRetrieval=").Append(this.MinimumTimeBetweenInventoryRetrieval);
            }

            if (this.Imprintlimit.HasValue)
            {
                serverArgs.Append(" -imprintlimit=").Append(this.Imprintlimit);
            }

            if (!string.IsNullOrWhiteSpace(this.Culture))
            {
                serverArgs.Append(" -culture=").Append(this.Culture);
            }

            if (this.NewSaveFormat)
            {
                serverArgs.Append(" -newsaveformat");
            }

            if (this.UseStore)
            {
                serverArgs.Append(" -usestore");
            }

            if (this.BackupTransferPlayerDatas)
            {
                serverArgs.Append(" -BackupTransferPlayerDatas");
            }

            if (this.MaxNumOfSaveBackups != 20)
            {
                serverArgs.Append(" -MaxNumOfSaveBackups=").Append(this.MaxNumOfSaveBackups);
            }
            
            if (this.NewYear1UTC.HasValue || this.NewYear2UTC.HasValue)
            {
                serverArgs.Append(" -NewYearEvent");
            }
            
            return serverArgs.ToString();
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

            if (profile.PlayerLevels.Count == 0)
            {
                profile.ResetLevelProgressionToOfficial(LevelProgression.Player);
                profile.EnableLevelProgressions = false;
            }
            if (profile.DinoLevels.Count == 0)
            {
                profile.ResetLevelProgressionToOfficial(LevelProgression.Dino);
                profile.EnableDinoLevelProgressions = false;
            }

            //
            // Since these are not inserted the normal way, we force a recomputation here.
            //
            profile.PlayerLevels.UpdateTotals();
            profile.DinoLevels.UpdateTotals();
            profile.DinoSettings.RenderToView();
            profile.EngramSettings.RenderToView();
            if (Config.Default.SectionMapSpawnerOverridesEnabled)
                profile.NPCSpawnSettings.RenderToView();
            if (Config.Default.SectionSupplyCrateOverridesEnabled)
                profile.ConfigOverrideSupplyCrateItems.RenderToView();
            if (Config.Default.SectionExcludeItemIndicesOverridesEnabled)
                profile.ExcludeItemIndices.RenderToView();
            if (Config.Default.SectionCraftingOverridesEnabled)
                profile.ConfigOverrideItemCraftingCosts.RenderToView();
            if (Config.Default.SectionStackSizeOverridesEnabled)
                profile.ConfigOverrideItemMaxQuantity.RenderToView();
            if (Config.Default.SectionPreventTransferOverridesEnabled)
                profile.PreventTransferForClassNames.RenderToView();

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

            var values = iniFile.ReadSection(IniFiles.Game, IniSections.Game_ShooterGameMode);

            var levelRampOverrides = values.Where(s => s.StartsWith("LevelExperienceRampOverrides=")).ToArray();
            if (levelRampOverrides.Length > 0)
            {
                var engramPointOverrides = values.Where(s => s.StartsWith("OverridePlayerLevelEngramPoints="));

                profile.EnableLevelProgressions = true;
                profile.PlayerLevels = LevelList.FromINIValues(levelRampOverrides[0], engramPointOverrides);

                if (levelRampOverrides.Length > 1)
                {
                    profile.EnableDinoLevelProgressions = true;
                    profile.DinoLevels = LevelList.FromINIValues(levelRampOverrides[1], null);
                }
            }

            return profile;
        }

        public static ServerProfile LoadFromProfileFile(string file, ServerProfile profile)
        {
            profile = LoadFromProfileFileBasic(file, profile);

            if (profile is null)
                return null;

            profile.CheckLauncherArgs();

            if (profile.PGM_Enabled)
                Config.Default.SectionPGMEnabled = true;

            if (profile.SOTF_Enabled)
                Config.Default.SectionSOTFEnabled = true;

            if (profile.PlayerLevels.Count == 0)
            {
                profile.ResetLevelProgressionToOfficial(LevelProgression.Player);
                profile.EnableLevelProgressions = false;
            }
            if (profile.DinoLevels.Count == 0)
            {
                profile.ResetLevelProgressionToOfficial(LevelProgression.Dino);
                profile.EnableDinoLevelProgressions = false;
            }

            //
            // Since these are not inserted the normal way, we force a recomputation here.
            //
            profile.PlayerLevels.UpdateTotals();
            profile.DinoLevels.UpdateTotals();
            profile.DinoSettings.RenderToView();
            profile.EngramSettings.RenderToView();
            if (Config.Default.SectionMapSpawnerOverridesEnabled)
                profile.NPCSpawnSettings.RenderToView();
            if (Config.Default.SectionSupplyCrateOverridesEnabled)
                profile.ConfigOverrideSupplyCrateItems.RenderToView();
            if (Config.Default.SectionExcludeItemIndicesOverridesEnabled)
                profile.ExcludeItemIndices.RenderToView();
            if (Config.Default.SectionCraftingOverridesEnabled)
                profile.ConfigOverrideItemCraftingCosts.RenderToView();
            if (Config.Default.SectionStackSizeOverridesEnabled)
                profile.ConfigOverrideItemMaxQuantity.RenderToView();
            if (Config.Default.SectionPreventTransferOverridesEnabled)
                profile.PreventTransferForClassNames.RenderToView();

            profile.LoadServerFiles(true, true, true);
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

            if (profile is null)
                profile = new ServerProfile();

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

            var serverConfigFile = Path.Combine(GetProfileServerConfigDir(profile), Config.Default.ServerGameUserSettingsConfigFile);
            if (File.Exists(serverConfigFile))
            {
                profile = LoadFromConfigFiles(serverConfigFile, profile, exclusions: null);
            }

            profile._lastSaveLocation = file;
            return profile;
        }

        public void Save(bool updateFolderPermissions, bool updateSchedules, ProgressDelegate progressCallback)
        {
            if (string.IsNullOrWhiteSpace(Config.Default.DataDir))
                return;

            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_Saving"));

            if (SOTF_Enabled)
            {
                BranchName = string.Empty;
                BranchPassword = string.Empty;
                EventName = string.Empty;
                EventColorsChanceOverride = 0;

                // ensure that the auto settings are switched off for SotF servers
                EnableAutoBackup = false;
                EnableAutoShutdown1 = false;
                RestartAfterShutdown1 = true;
                EnableAutoShutdown2 = false;
                RestartAfterShutdown2 = true;
                EnableAutoUpdate = false;
                AutoRestartIfShutdown = false;

                // ensure the procedurally generated settings are switched off for SotF servers
                PGM_Enabled = false;
            }

            UpdateProfileToolTip();
            ClearSteamAppManifestBranch();
            CheckLauncherArgs();

            // check if the processor affinity is valid
            if (!ProcessUtils.IsProcessorAffinityValid(ProcessAffinity))
                // processor affinity is not valid, set back to 0 (default ALL processors)
                ProcessAffinity = 0;

            if (!EngramSettings.IsEnabled)
                OnlyAllowSpecifiedEngrams = false;
            this.EngramSettings.OnlyAllowSpecifiedEngrams = OnlyAllowSpecifiedEngrams;

            // ensure that the extinction event date is cleared if the extinction event is disabled
            if (!EnableExtinctionEvent)
            {
                ClearValue(ExtinctionEventUTCProperty);
            }

            // ensure that the MAX XP settings for player and dinos are set to the last custom level
            if (EnableLevelProgressions)
            {
                progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_CheckingPlayerDinoMaxXP"));

                // players
                var list = GetLevelList(LevelProgression.Player);
                var lastxp = (list == null || list.Count == 0) ? 0 : list[list.Count - 1].XPRequired;

                if (lastxp > 0 && !OverrideMaxExperiencePointsPlayer.Equals(lastxp))
                {
                    OverrideMaxExperiencePointsPlayer.SetValue(lastxp);
                }

                if (EnableDinoLevelProgressions)
                {
                    // dinos
                    list = GetLevelList(LevelProgression.Dino);
                    lastxp = (list == null || list.Count == 0) ? 0 : list[list.Count - 1].XPRequired;

                    if (lastxp > 0 && !OverrideMaxExperiencePointsDino.Equals(lastxp))
                    {
                        OverrideMaxExperiencePointsDino.SetValue(lastxp);
                    }
                }
            }

            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_ConstructingDinoInformation"));
            this.DinoSettings.RenderToModel();

            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_ConstructingEngramInformation"));
            this.EngramSettings.RenderToModel();

            if (Config.Default.SectionMapSpawnerOverridesEnabled)
            {
                progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_ConstructingMapSpawnerInformation"));
                this.NPCSpawnSettings.RenderToModel();
            }

            if (Config.Default.SectionSupplyCrateOverridesEnabled)
            {
                progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_ConstructingSupplyCrateInformation"));
                this.ConfigOverrideSupplyCrateItems.RenderToModel();
            }

            if (Config.Default.SectionExcludeItemIndicesOverridesEnabled)
            {
                progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_ConstructingExcludeItemIndicesInformation"));
                this.ExcludeItemIndices.RenderToModel();
            }

            if (Config.Default.SectionCraftingOverridesEnabled)
            {
                progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_ConstructingCraftingOverridesInformation"));
                this.ConfigOverrideItemCraftingCosts.RenderToModel();
            }

            if (Config.Default.SectionStackSizeOverridesEnabled)
            {
                progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_ConstructingStackSizeInformation"));
                this.ConfigOverrideItemMaxQuantity.RenderToModel();
            }

            if (Config.Default.SectionPreventTransferOverridesEnabled)
            {
                progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_ConstructingPreventTransferInformation"));
                this.PreventTransferForClassNames.RenderToModel();
            }

            if (!Config.Default.SectionPGMEnabled)
            {
                PGM_Enabled = false;
            }

            if (!Config.Default.SectionSOTFEnabled)
            {
                SOTF_Enabled = false;
            }

            //
            // Save the profile
            //
            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_SavingProfileFile"));
            SaveProfile();

            try
            {
                DestroyServerFilesWatcher();

                SaveServerFileAdministrators();
                SaveServerFileExclusive();
                SaveServerFileWhitelisted();
            }
            finally
            {
                SetupServerFilesWatcher();
            }

            //
            // Write the INI files
            //
            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_SavingConfigFiles"));
            SaveConfigFiles();

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

            progressCallback?.Invoke(0, _globalizer.GetResourceString("ProfileSave_CheckingWebAlarmFile"));
            UpdateWebAlarm();

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
            string configDir = GetProfileServerConfigDir();
            Directory.CreateDirectory(configDir);
            SaveConfigFile(configDir);
        }

        public void SaveConfigFile(string profileIniDir, IEnumerable<Enum> exclusions = null)
        {
            if (exclusions == null)
                exclusions = GetExclusions();

            var iniFile = new SystemIniFile(profileIniDir);
            iniFile.Serialize(this, exclusions);

            var values = iniFile.ReadSection(IniFiles.Game, IniSections.Game_ShooterGameMode);

            var filteredValues = values.Where(s => !s.StartsWith("LevelExperienceRampOverrides=") && !s.StartsWith("OverridePlayerLevelEngramPoints=")).ToList();
            if (EnableLevelProgressions)
            {
                //
                // These must be added in this order: Player Levels, then Dino Levels (optional), then Player Engrams, per the ARK INI file format.
                //
                filteredValues.Add(this.PlayerLevels.ToINIValueForXP());
                if (EnableDinoLevelProgressions)
                {
                    filteredValues.Add(this.DinoLevels.ToINIValueForXP());
                }
                filteredValues.AddRange(this.PlayerLevels.ToINIValuesForEngramPoints());
            }

            iniFile.WriteSection(IniFiles.Game, IniSections.Game_ShooterGameMode, filteredValues);
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

        private void UpdateWebAlarm()
        {
            var alarmPostCredentialsFile = Path.Combine(this.InstallDirectory, Config.Default.SavedRelativePath, Config.Default.WebAlarmFile);

            try
            {
                // check if the web alarm option is enabled.
                if (this.EnableWebAlarm)
                {
                    // check if the directory exists.
                    if (!Directory.Exists(Path.GetDirectoryName(alarmPostCredentialsFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(alarmPostCredentialsFile));

                    var contents = new StringBuilder();
                    contents.AppendLine($"{this.WebAlarmKey}");
                    contents.AppendLine($"{this.WebAlarmUrl}");
                    File.WriteAllText(alarmPostCredentialsFile, contents.ToString());
                }
                else
                {
                    // check if the files exists and delete it.
                    if (File.Exists(alarmPostCredentialsFile))
                        File.Delete(alarmPostCredentialsFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_WebAlarmErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool Validate(bool forceValidate, out string validationMessage)
        {
            validationMessage = string.Empty;
            StringBuilder result = new StringBuilder();

            var appId = SOTF_Enabled ? Config.Default.AppId_SotF : Config.Default.AppId;

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
            if (RCONPort < ushort.MinValue || RCONPort > ushort.MaxValue)
            {
                var message = _globalizer.GetResourceString("ProfileValidation_RconPort")?.Replace("{PortMinimum}", ushort.MinValue.ToString()).Replace("{PortMaximum}", ushort.MaxValue.ToString());
                result.AppendLine(message);
            }

            if (forceValidate || Config.Default.ValidateProfileOnServerStart)
            {
                // build a list of mods to be processed
                var serverMapModId = GetProfileMapModId(this);
                var serverMapName = GetProfileMapName(this);
                var modIds = ModUtils.GetModIdList(ServerModIds);
                modIds = ModUtils.ValidateModList(modIds);

                var modIdList = new List<string>();
                if (!string.IsNullOrWhiteSpace(serverMapModId))
                    modIdList.Add(serverMapModId);
                if (!string.IsNullOrWhiteSpace(TotalConversionModId))
                    modIdList.Add(TotalConversionModId);
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
                var serverFile = Path.Combine(serverFolder, Config.Default.ServerExe);
                if (!Directory.Exists(serverFolder))
                {
                    var message = _globalizer.GetResourceString("ProfileValidation_ServerNotDownloaded");
                    result.AppendLine(message);
                }
                else if (!File.Exists(serverFile))
                {
                    var message = _globalizer.GetResourceString("ProfileValidation_ServerExeNotDownloaded")?.Replace("{ServerExe}", Config.Default.ServerExe);
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

                // check if the map is a mod and confirm the map name.
                if (!string.IsNullOrWhiteSpace(serverMapModId))
                {
                    var modFolder = ModUtils.GetModPath(InstallDirectory, serverMapModId);
                    if (!Directory.Exists(modFolder))
                    {
                        var message = _globalizer.GetResourceString("ProfileValidation_MapModNotDownloaded");
                        result.AppendLine(message);
                    }
                    else if (!File.Exists($"{modFolder}.mod"))
                    {
                        var message = _globalizer.GetResourceString("ProfileValidation_MapModFileNotDownloaded");
                        result.AppendLine(message);
                    }
                    else
                    {
                        var modType = ModUtils.GetModType(InstallDirectory, serverMapModId);
                        if (modType == ModUtils.MODTYPE_UNKNOWN)
                        {
                            var message = _globalizer.GetResourceString("ProfileValidation_MapModFileInvalid");
                            result.AppendLine(message);
                        }
                        else if (modType != ModUtils.MODTYPE_MAP)
                        {
                            var message = _globalizer.GetResourceString("ProfileValidation_MapModInvalid");
                            result.AppendLine(message);
                        }
                        else
                        {
                            // do not process any mods that are not included in the mod list.
                            if (modIdList.Contains(serverMapModId))
                            {
                                var mapName = ModUtils.GetMapName(InstallDirectory, serverMapModId);
                                if (string.IsNullOrWhiteSpace(mapName))
                                {
                                    var message = _globalizer.GetResourceString("ProfileValidation_MapModNameInvalid");
                                    result.AppendLine(message);
                                }
                                else if (!mapName.Equals(serverMapName))
                                {
                                    var message = _globalizer.GetResourceString("ProfileValidation_MapModNameMismatch");
                                    result.AppendLine(message);
                                }
                                else
                                {
                                    var modDetail = modDetails?.publishedfiledetails?.FirstOrDefault(d => d.publishedfileid.Equals(serverMapModId));
                                    if (modDetail != null && modDetail.consumer_app_id != null)
                                    {
                                        if (!modDetail.consumer_app_id.Equals(appId))
                                        {
                                            var message = _globalizer.GetResourceString("ProfileValidation_MapModDifferentApplication");
                                            result.AppendLine(message);
                                        }
                                        else
                                        {
                                            var modVersion = ModUtils.GetModLatestTime(ModUtils.GetLatestModTimeFile(InstallDirectory, serverMapModId));
                                            if (!modVersion.Equals(modDetail.time_updated))
                                            {
                                                var message = _globalizer.GetResourceString("ProfileValidation_MapModOutdated");
                                                result.AppendLine(message);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var message = _globalizer.GetResourceString("ProfileValidation_MapModSteamError");
                                        result.AppendLine(message);
                                    }
                                }
                            }
                        }
                    }
                }

                // check for a total conversion mod
                if (!string.IsNullOrWhiteSpace(TotalConversionModId))
                {
                    var modFolder = ModUtils.GetModPath(InstallDirectory, TotalConversionModId);
                    if (!Directory.Exists(modFolder))
                    {
                        var message = _globalizer.GetResourceString("ProfileValidation_TCModNotDownloaded");
                        result.AppendLine(message);
                    }
                    else if (!File.Exists($"{modFolder}.mod"))
                    {
                        var message = _globalizer.GetResourceString("ProfileValidation_TCModFileNotDownloaded");
                        result.AppendLine(message);
                    }
                    else
                    {
                        var modType = ModUtils.GetModType(InstallDirectory, TotalConversionModId);
                        if (modType == ModUtils.MODTYPE_UNKNOWN)
                        {
                            var message = _globalizer.GetResourceString("ProfileValidation_TCModFileInvalid");
                            result.AppendLine(message);
                        }
                        else if (modType != ModUtils.MODTYPE_TOTCONV)
                        {
                            var message = _globalizer.GetResourceString("ProfileValidation_TCModInvalid");
                            result.AppendLine(message);
                        }
                        else
                        {
                            // do not process any mods that are not included in the mod list.
                            if (modIdList.Contains(TotalConversionModId))
                            {
                                var mapName = ModUtils.GetMapName(InstallDirectory, TotalConversionModId);
                                if (string.IsNullOrWhiteSpace(mapName))
                                {
                                    var message = _globalizer.GetResourceString("ProfileValidation_TCModNameInvalid");
                                    result.AppendLine(message);
                                }
                                else if (!mapName.Equals(serverMapName))
                                {
                                    var message = _globalizer.GetResourceString("ProfileValidation_TCModNameMismatch");
                                    result.AppendLine(message);
                                }
                                else
                                {
                                    var modDetail = modDetails?.publishedfiledetails?.FirstOrDefault(d => d.publishedfileid.Equals(TotalConversionModId));
                                    if (modDetail != null && modDetail.consumer_app_id != null)
                                    {
                                        if (!modDetail.consumer_app_id.Equals(appId))
                                        {
                                            var message = _globalizer.GetResourceString("ProfileValidation_TCModDifferentApplication");
                                            result.AppendLine(message);
                                        }
                                        else
                                        {
                                            var modVersion = ModUtils.GetModLatestTime(ModUtils.GetLatestModTimeFile(InstallDirectory, TotalConversionModId));
                                            if (!modVersion.Equals(modDetail.time_updated))
                                            {
                                                var message = _globalizer.GetResourceString("ProfileValidation_TCModOutdated");
                                                result.AppendLine(message);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var message = _globalizer.GetResourceString("ProfileValidation_TCModSteamError");
                                        result.AppendLine(message);
                                    }
                                }
                            }
                        }
                    }
                }

                // check for the mods
                foreach (var modId in modIds)
                {
                    var modFolder = ModUtils.GetModPath(InstallDirectory, modId);
                    if (!Directory.Exists(modFolder))
                    {
                        var message = _globalizer.GetResourceString("ProfileValidation_ModNotDownloaded")?.Replace("{modId}", modId);
                        result.AppendLine(message);
                    }
                    else if (!File.Exists($"{modFolder}.mod"))
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

            var mapName = GetProfileMapFileName(this);
            var worldFileName = $"{mapName}{Config.Default.MapExtension}";
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
                    worldFile
                };

                if (restoreAll)
                {
                    var saveFolderInfo = new DirectoryInfo(saveFolder);

                    // get the player files
                    var playerFileFilter = $"*{Config.Default.PlayerFileExtension}";
                    var playerFiles = saveFolderInfo.GetFiles(playerFileFilter, SearchOption.TopDirectoryOnly);
                    foreach (var file in playerFiles)
                    {
                        files.Add(file.FullName);
                    }

                    // get the tribe files
                    var tribeFileFilter = $"*{Config.Default.TribeFileExtension}";
                    var tribeFiles = saveFolderInfo.GetFiles(tribeFileFilter, SearchOption.TopDirectoryOnly);
                    foreach (var file in tribeFiles)
                    {
                        files.Add(file.FullName);
                    }

                    // get the tribute tribe files
                    var tributeTribeFileFilter = $"*{Config.Default.TributeTribeFileExtension}";
                    var tributeTribeFiles = saveFolderInfo.GetFiles(tributeTribeFileFilter, SearchOption.TopDirectoryOnly);
                    foreach (var file in tributeTribeFiles)
                    {
                        files.Add(file.FullName);
                    }

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

                // restore the files from the backup
                if (restoreAll)
                {
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
            ServerNameLengthToLong = ServerNameLength > 50;
        }

        public void ValidateMOTD()
        {
            MOTDLength = Encoding.UTF8.GetByteCount(MOTD);
            MOTDLengthToLong = MOTDLength > 1000;

            MOTDLineCount = string.IsNullOrWhiteSpace(MOTD) ? 0 : MOTD.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length;
            MOTDLineCountToLong = MOTDLineCount > 7;
        }

        private void ClearNullableValue(DependencyProperty dp)
        {
            var defaultValue = dp.DefaultMetadata.DefaultValue as INullableValue;
            this.SetValue(dp, defaultValue?.Clone());
        }

        private void SetNullableValue(DependencyProperty dp, object value)
        {
            var newValue = value as INullableValue;
            this.SetValue(dp, newValue?.Clone());
        }

        #endregion

        #region Export Methods
        public void ExportDinoLevels(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            LevelList list = GetLevelList(LevelProgression.Dino);

            StringBuilder output = new StringBuilder();
            foreach (var level in list)
            {
                output.AppendLine($"{level.LevelIndex}{CSV_DELIMITER}{level.XPRequired}");
            }

            File.WriteAllText(fileName, output.ToString());
        }

        public void ExportPlayerLevels(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            LevelList list = GetLevelList(LevelProgression.Player);

            StringBuilder output = new StringBuilder();
            foreach (var level in list)
            {
                output.AppendLine($"{level.LevelIndex}{CSV_DELIMITER}{level.XPRequired}{CSV_DELIMITER}{level.EngramPoints}");
            }

            File.WriteAllText(fileName, output.ToString());
        }
        #endregion

        #region Import Methods
        public void ImportDinoLevels(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) && !File.Exists(fileName))
                return;

            var csvParserOptions = new CsvParserOptions(false, CSV_DELIMITER);
            var csvMapper = new CsvDinoLevelMapping();
            var csvParser = new CsvParser<ImportLevel>(csvParserOptions, csvMapper);

            var result = csvParser.ReadFromFile(fileName, Encoding.ASCII).ToList();
            if (result.Any(r => !r.IsValid))
            {
                var error = result.First(r => r.Error != null);
                throw new Exception($"Import error occured in column {error.Error.ColumnIndex} with a value of {error.Error.Value}");
            }

            var list = GetLevelList(LevelProgression.Dino);
            list.Clear();

            foreach (var level in result)
            {
                list.Add(level.Result.AsLevel());
            }

            list.UpdateTotals();
        }

        public void ImportPlayerLevels(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) && !File.Exists(fileName))
                return;

            var csvParserOptions = new CsvParserOptions(false, CSV_DELIMITER);
            var csvMapper = new CsvPlayerLevelMapping();
            var csvParser = new CsvParser<ImportLevel>(csvParserOptions, csvMapper);

            var result = csvParser.ReadFromFile(fileName, Encoding.ASCII).ToList();
            if (result.Any(r => !r.IsValid))
            {
                var error = result.First(r => r.Error != null);
                throw new Exception($"Import error occured in column {error.Error.ColumnIndex} with a value of {error.Error.Value}");
            }

            var list = GetLevelList(LevelProgression.Player);
            list.Clear();

            foreach (var level in result)
            {
                list.Add(level.Result.AsLevel());
            }

            list.UpdateTotals();
        }
        #endregion

        #region Reset Methods
        public void ClearLevelProgression(LevelProgression levelProgression)
        {
            var list = GetLevelList(levelProgression);
            list.Clear();

            list.Add(new Level { LevelIndex = 0, XPRequired = 1, EngramPoints = 0 });

            list.UpdateTotals();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SCS0005:Weak random number generator.", Justification = "<Pending>")]
        public void RandomizePGMSettings()
        {
            var random = new Random(DateTime.Now.Millisecond);

            this.PGM_Terrain.MapSeed = random.Next(1, 999);

            this.PGM_Terrain.WaterFrequency = (float)Math.Round(random.NextDouble() * 5f, 5);
            this.PGM_Terrain.MountainsFrequency = (float)Math.Round(random.NextDouble() * 10f, 5);
            if (this.PGM_Terrain.MountainsFrequency < 3.0f)
                this.PGM_Terrain.MountainsFrequency += 3.0f;

            this.PGM_Terrain.MountainsSlope = (float)Math.Round(random.NextDouble() + 0.3f, 5);
            if (this.PGM_Terrain.MountainsSlope < 0.5f)
                this.PGM_Terrain.MountainsSlope += 0.5f;
            this.PGM_Terrain.MountainsHeight = (float)Math.Round(random.NextDouble() + 0.3f, 5);
            if (this.PGM_Terrain.MountainsHeight < 0.5f)
                this.PGM_Terrain.MountainsHeight += 0.5f;

            this.PGM_Terrain.SnowBiomeSize = (float)Math.Round(random.NextDouble(), 5);
            this.PGM_Terrain.RedWoodBiomeSize = (float)Math.Round(random.NextDouble(), 5);
            if (this.PGM_Terrain.RedWoodBiomeSize > 0.5f)
                this.PGM_Terrain.RedWoodBiomeSize -= 0.5f;

            this.PGM_Terrain.MountainBiomeStart = -(float)Math.Round(random.NextDouble(), 5);
            this.PGM_Terrain.JungleBiomeStart = -(float)Math.Round(random.NextDouble(), 5);

            this.PGM_Terrain.GrassDensity = (float)Math.Round(random.Next(80, 100) / 100.1f, 3);
            this.PGM_Terrain.JungleGrassDensity = (float)Math.Round(random.Next(2, 9) / 100.1f, 3);
            this.PGM_Terrain.MountainGrassDensity = (float)Math.Round(random.Next(3, 10) / 100.1f, 3);
            this.PGM_Terrain.RedwoodGrassDensity = (float)Math.Round(random.Next(5, 15) / 100.1f, 3);
            this.PGM_Terrain.SnowGrassDensity = (float)Math.Round(random.Next(10, 30) / 100.1f, 3);
            this.PGM_Terrain.SnowMountainGrassDensity = (float)Math.Round(random.Next(10, 20) / 100.1f, 3);

            this.PGM_Terrain.TreeDensity = (float)Math.Round(random.Next(11, 135) / 10000.1f, 3);
            this.PGM_Terrain.JungleTreeDensity = (float)Math.Round(random.Next(48, 83) / 100.1f, 3);
            this.PGM_Terrain.MountainsTreeDensity = (float)Math.Round(random.Next(9, 16) / 1000.1f, 3);
            this.PGM_Terrain.RedWoodTreeDensity = (float)Math.Round(random.Next(23, 51) / 100.1f, 3);
            this.PGM_Terrain.SnowTreeDensity = (float)Math.Round(random.Next(80, 100) / 100.1f, 3);
            this.PGM_Terrain.SnowMountainsTreeDensity = (float)Math.Round(random.Next(9, 16) / 1000.1f, 3);
            this.PGM_Terrain.ShoreTreeDensity = (float)Math.Round(random.Next(48, 83) / 1000.1f, 3);
            this.PGM_Terrain.SnowShoreTreeDensity = (float)Math.Round(random.Next(14, 31) / 1000.1f, 3);

            this.PGM_Terrain.InlandWaterObjectsDensity = (float)Math.Round(random.Next(36, 67) / 100.1f, 3);
            this.PGM_Terrain.UnderwaterObjectsDensity = (float)Math.Round(random.Next(36, 67) / 100.1f, 3);
        }

        public void ResetLevelProgressionToOfficial(LevelProgression levelProgression)
        {
            var list = GetLevelList(levelProgression);
            list.Clear();

            switch (levelProgression)
            {
                case LevelProgression.Player:
                    list.AddRange(GameData.LevelsPlayer);
                    break;
                case LevelProgression.Dino:
                    list.AddRange(GameData.LevelsDino);
                    break;
            }

            list.UpdateTotals();
        }

        public void ResetProfileId()
        {
            this.ProfileID = Guid.NewGuid().ToString();
        }

        // individual value reset methods
        public void ResetBanlist()
        {
            this.ClearValue(EnableBanListURLProperty);
            this.ClearValue(BanListURLProperty);
        }

        public void ResetMapName(string mapName)
        {
            this.ServerMap = mapName;
        }

        public void ResetOverrideMaxExperiencePointsDino()
        {
            OverrideMaxExperiencePointsDino.SetValue(GameData.DefaultMaxExperiencePointsDino);
        }

        public void ResetOverrideMaxExperiencePointsPlayer()
        {
            OverrideMaxExperiencePointsPlayer.SetValue(GameData.DefaultMaxExperiencePointsPlayer);
        }

        public void ResetRCONWindowExtents()
        {
            this.ClearValue(RCONWindowExtentsProperty);
        }

        public void ResetServerOptions()
        {
            this.ClearValue(CultureProperty);
            this.ClearValue(DisableValveAntiCheatSystemProperty);
            this.ClearValue(DisablePlayerMovePhysicsOptimizationProperty);
            this.ClearValue(DisableAntiSpeedHackDetectionProperty);
            this.ClearValue(SpeedHackBiasProperty);
            this.ClearValue(UseBattlEyeProperty);

            this.ClearValue(ForceRespawnDinosProperty);
            this.ClearValue(EnableServerAutoForceRespawnWildDinosIntervalProperty);
            this.ClearValue(ServerAutoForceRespawnWildDinosIntervalProperty);
            this.ClearValue(EnableServerAdminLogsProperty);
            this.ClearValue(MaxTribeLogsProperty);
            this.ClearValue(ForceDirectX10Property);
            this.ClearValue(ForceShaderModel4Property);
            this.ClearValue(ForceLowMemoryProperty);
            this.ClearValue(ForceNoManSkyProperty);
            this.ClearValue(UseNoMemoryBiasProperty);
            this.ClearValue(UseNoHangDetectionProperty);
            this.ClearValue(ServerAllowAnselProperty);
            this.ClearValue(StructureMemoryOptimizationsProperty);
            this.ClearValue(UseStructureStasisGridProperty);
            this.ClearValue(NoUnderMeshCheckingProperty);
            this.ClearValue(NoUnderMeshKillingProperty);
            this.ClearValue(NoDinosProperty);
            this.ClearValue(UseVivoxProperty);
            this.ClearValue(AllowSharedConnectionsProperty);
            this.ClearValue(CrossplayProperty);
            this.ClearValue(EpicOnlyProperty);
            this.ClearValue(EnablePublicIPForEpicProperty);
            this.ClearValue(OutputServerLogProperty);
            this.ClearValue(SecureSendArKPayloadProperty);
            this.ClearValue(UseItemDupeCheckProperty);
            this.ClearValue(UseSecureSpawnRulesProperty);

            this.ClearValue(AltSaveDirectoryNameProperty);
            this.ClearValue(CrossArkClusterIdProperty);
            this.ClearValue(ClusterDirOverrideProperty);
        }

        public void ResetServerBadWordFilterOptions()
        {
            this.ClearValue(EnableBadWordListURLProperty);
            this.ClearValue(EnableBadWordWhiteListURLProperty);
            this.ClearValue(BadWordListURLProperty);
            this.ClearValue(BadWordWhiteListURLProperty);
            this.ClearValue(FilterTribeNamesProperty);
            this.ClearValue(FilterCharacterNamesProperty);
            this.ClearValue(FilterChatProperty);
        }

        public void ResetServerLogOptions()
        {
            this.ClearValue(EnableServerAdminLogsProperty);
            this.ClearValue(MaxTribeLogsProperty);
            this.ClearValue(ServerAdminLogsIncludeTribeLogsProperty);
            this.ClearValue(ServerRCONOutputTribeLogsProperty);
            this.ClearValue(NotifyAdminCommandsInChatProperty);
            this.ClearValue(TribeLogDestroyedEnemyStructuresProperty);
            this.ClearValue(AllowHideDamageSourceFromLogsProperty);
        }

        // section reset methods
        public void ResetAdministrationSection()
        {
            this.ClearValue(ServerNameProperty);
            this.ClearValue(ServerPasswordProperty);
            this.ClearValue(AdminPasswordProperty);
            this.ClearValue(SpectatorPasswordProperty);
            this.ClearValue(ServerPortProperty);
            this.ClearValue(ServerPeerPortProperty);
            this.ClearValue(QueryPortProperty);
            this.ClearValue(ServerIPProperty);

            UpdatePortsString();

            this.ClearValue(EnableBanListURLProperty);
            this.ClearValue(BanListURLProperty);
            this.ClearValue(EnableCustomDynamicConfigUrlProperty);
            this.ClearValue(CustomDynamicConfigUrlProperty);
            this.ClearValue(EnableCustomLiveTuningUrlProperty);
            this.ClearValue(CustomLiveTuningUrlProperty);
            this.ClearValue(MaxPlayersProperty);
            this.ClearNullableValue(KickIdlePlayersPeriodProperty);

            this.ClearValue(RCONEnabledProperty);
            this.ClearValue(RCONPortProperty);
            this.ClearValue(RCONServerGameLogBufferProperty);
            this.ClearValue(AdminLoggingProperty);

            this.ClearValue(ServerMapProperty);
            this.ClearValue(TotalConversionModIdProperty);
            this.ClearValue(ServerModIdsProperty);

            this.ClearValue(EnableExtinctionEventProperty);
            this.ClearValue(ExtinctionEventTimeIntervalProperty);
            this.ClearValue(ExtinctionEventUTCProperty);

            this.ClearValue(AutoSavePeriodMinutesProperty);
            this.ClearValue(MaxNumOfSaveBackupsProperty);

            this.ClearValue(MOTDProperty);
            this.ClearValue(MOTDDurationProperty);
            this.ClearNullableValue(MOTDIntervalProperty);

            ResetServerOptions();
            ResetServerBadWordFilterOptions();
            ResetServerLogOptions();

            this.ClearValue(EnableWebAlarmProperty);
            this.ClearValue(WebAlarmKeyProperty);
            this.ClearValue(WebAlarmUrlProperty);

            this.ClearValue(CrossArkClusterIdProperty);
            this.ClearValue(ClusterDirOverrideProperty);

            this.ClearValue(ProcessPriorityProperty);
            this.ClearValue(ProcessAffinityProperty);

            this.ClearValue(AdditionalArgsProperty);
            this.ClearValue(LauncherArgsOverrideProperty);
            this.ClearValue(LauncherArgsPrefixProperty);
            this.ClearValue(LauncherArgsProperty);

            this.ClearValue(NewSaveFormatProperty);
            this.ClearValue(UseStoreProperty);
            this.ClearValue(BackupTransferPlayerDatasProperty);
        }

        public void ResetChatAndNotificationSection()
        {
            this.ClearValue(EnableGlobalVoiceChatProperty);
            this.ClearValue(EnableProximityChatProperty);
            this.ClearValue(EnablePlayerLeaveNotificationsProperty);
            this.ClearValue(EnablePlayerJoinedNotificationsProperty);
        }

        public void ResetCraftingOverridesSection()
        {
            this.ConfigOverrideItemCraftingCosts = new CraftingOverrideList(nameof(ConfigOverrideItemCraftingCosts));
            this.ConfigOverrideItemCraftingCosts.Reset();
        }

        public void ResetCustomLevelsSection()
        {
            this.ClearNullableValue(OverrideMaxExperiencePointsPlayerProperty);
            this.ClearNullableValue(OverrideMaxExperiencePointsDinoProperty);

            this.ClearValue(EnableLevelProgressionsProperty);
            this.ClearValue(EnableDinoLevelProgressionsProperty);

            this.PlayerLevels = new LevelList();
            this.ResetLevelProgressionToOfficial(LevelProgression.Player);

            this.DinoLevels = new LevelList();
            this.ResetLevelProgressionToOfficial(LevelProgression.Dino);
        }

        public void ResetDinoSettingsSection()
        {
            this.ClearValue(DinoDamageMultiplierProperty);
            this.ClearValue(TamedDinoDamageMultiplierProperty);
            this.ClearValue(DinoResistanceMultiplierProperty);
            this.ClearValue(TamedDinoResistanceMultiplierProperty);
            this.ClearValue(DinoCharacterFoodDrainMultiplierProperty);
            this.ClearValue(DinoCharacterStaminaDrainMultiplierProperty);
            this.ClearValue(DinoCharacterHealthRecoveryMultiplierProperty);
            this.ClearValue(DinoHarvestingDamageMultiplierProperty);
            this.ClearValue(DinoTurretDamageMultiplierProperty);

            this.ClearValue(AllowRaidDinoFeedingProperty);
            this.ClearValue(RaidDinoCharacterFoodDrainMultiplierProperty);

            this.ClearValue(EnableAllowCaveFlyersProperty);
            this.ClearValue(AllowFlyingStaminaRecoveryProperty);
            this.ClearValue(AllowFlyerSpeedLevelingProperty);
            this.ClearValue(PreventMateBoostProperty);

            this.ClearValue(DisableDinoDecayPvEProperty);
            this.ClearValue(DisableDinoDecayPvPProperty);
            this.ClearValue(AutoDestroyDecayedDinosProperty);
            this.ClearValue(UseDinoLevelUpAnimationsProperty);
            this.ClearValue(PvEDinoDecayPeriodMultiplierProperty);
            this.ClearValue(AllowMultipleAttachedC4Property);
            this.ClearValue(AllowUnclaimDinosProperty);

            this.ClearValue(DisableDinoRidingProperty);
            this.ClearValue(DisableDinoTamingProperty);
            this.ClearValue(DisableDinoBreedingProperty);
            this.ClearValue(MaxTamedDinosProperty);
            this.ClearValue(MaxPersonalTamedDinosProperty);
            this.ClearValue(PersonalTamedDinosSaddleStructureCostProperty);
            this.ClearValue(UseTameLimitForStructuresOnlyProperty);
            this.ClearValue(EnableForceCanRideFliersProperty);
            this.ClearValue(ForceCanRideFliersProperty);

            this.ClearValue(MatingIntervalMultiplierProperty);
            this.ClearValue(MatingSpeedMultiplierProperty);
            this.ClearValue(EggHatchSpeedMultiplierProperty);
            this.ClearValue(BabyMatureSpeedMultiplierProperty);
            this.ClearValue(BabyFoodConsumptionSpeedMultiplierProperty);

            this.ClearValue(DisableImprintDinoBuffProperty);
            this.ClearValue(AllowAnyoneBabyImprintCuddleProperty);
            this.ClearValue(BabyImprintingStatScaleMultiplierProperty);
            this.ClearValue(BabyImprintAmountMultiplierProperty);
            this.ClearValue(BabyCuddleIntervalMultiplierProperty);
            this.ClearValue(BabyCuddleGracePeriodMultiplierProperty);
            this.ClearValue(BabyCuddleLoseImprintQualitySpeedMultiplierProperty);
            this.ClearNullableValue(ImprintlimitProperty);

            this.ClearValue(WildDinoCharacterFoodDrainMultiplierProperty);
            this.ClearValue(TamedDinoCharacterFoodDrainMultiplierProperty);
            this.ClearValue(WildDinoTorporDrainMultiplierProperty);
            this.ClearValue(TamedDinoTorporDrainMultiplierProperty);
            this.ClearValue(PassiveTameIntervalMultiplierProperty);

            this.PerLevelStatsMultiplier_DinoWild = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoWild), GameData.GetPerLevelStatsMultipliers_DinoWild, GameData.GetStatMultiplierInclusions_DinoWildPerLevel(), true);
            this.PerLevelStatsMultiplier_DinoTamed = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoTamed), GameData.GetPerLevelStatsMultipliers_DinoTamed, GameData.GetStatMultiplierInclusions_DinoTamedPerLevel(), true);
            this.PerLevelStatsMultiplier_DinoTamed_Add = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoTamed_Add), GameData.GetPerLevelStatsMultipliers_DinoTamedAdd, GameData.GetStatMultiplierInclusions_DinoTamedAdd(), true);
            this.PerLevelStatsMultiplier_DinoTamed_Affinity = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity), GameData.GetPerLevelStatsMultipliers_DinoTamedAffinity, GameData.GetStatMultiplierInclusions_DinoTamedAffinity(), true);
            this.MutagenLevelBoost = new StatsMultiplierIntegerArray(nameof(MutagenLevelBoost), GameData.GetPerLevelMutagenLevelBoost_DinoWild, GameData.GetMutagenLevelBoostInclusions_DinoWild(), true);
            this.MutagenLevelBoost_Bred = new StatsMultiplierIntegerArray(nameof(MutagenLevelBoost_Bred), GameData.GetPerLevelMutagenLevelBoost_DinoTamed, GameData.GetMutagenLevelBoostInclusions_DinoTamed(), true);

            this.DinoSpawnWeightMultipliers = new AggregateIniValueList<DinoSpawn>(nameof(DinoSpawnWeightMultipliers), GameData.GetDinoSpawns);
            this.PreventDinoTameClassNames = new StringIniValueList(nameof(PreventDinoTameClassNames), () => new string[0]);
            this.PreventBreedingForClassNames = new StringIniValueList(nameof(PreventBreedingForClassNames), () => new string[0]);
            this.NPCReplacements = new AggregateIniValueList<NPCReplacement>(nameof(NPCReplacements), GameData.GetNPCReplacements);
            this.TamedDinoClassDamageMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(TamedDinoClassDamageMultipliers), GameData.GetDinoMultipliers);
            this.TamedDinoClassResistanceMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(TamedDinoClassResistanceMultipliers), GameData.GetDinoMultipliers);
            this.DinoClassDamageMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(DinoClassDamageMultipliers), GameData.GetDinoMultipliers);
            this.DinoClassResistanceMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(DinoClassResistanceMultipliers), GameData.GetDinoMultipliers);

            this.DinoSettings = new DinoSettingsList(this.DinoSpawnWeightMultipliers, this.PreventDinoTameClassNames, this.PreventBreedingForClassNames, this.NPCReplacements, this.TamedDinoClassDamageMultipliers, this.TamedDinoClassResistanceMultipliers, this.DinoClassDamageMultipliers, this.DinoClassResistanceMultipliers);
            this.DinoSettings.RenderToView();
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

        public void ResetEngramsSection()
        {
            this.ClearValue(AutoUnlockAllEngramsProperty);
            this.ClearValue(OnlyAllowSpecifiedEngramsProperty);

            this.OverrideNamedEngramEntries = new EngramEntryList(nameof(OverrideNamedEngramEntries));
            this.OverrideNamedEngramEntries.Reset();

            this.EngramEntryAutoUnlocks = new EngramAutoUnlockList(nameof(EngramEntryAutoUnlocks));
            this.EngramEntryAutoUnlocks.Reset();

            this.EngramSettings = new EngramSettingsList(this.OverrideNamedEngramEntries, this.EngramEntryAutoUnlocks);
        }

        public void ResetEnvironmentSection()
        {
            this.ClearValue(DinoCountMultiplierProperty);
            this.ClearValue(TamingSpeedMultiplierProperty);
            this.ClearValue(HarvestAmountMultiplierProperty);
            this.ClearValue(ResourcesRespawnPeriodMultiplierProperty);
            this.ClearValue(ResourceNoReplenishRadiusPlayersProperty);
            this.ClearValue(ResourceNoReplenishRadiusStructuresProperty);
            this.ClearValue(HarvestHealthMultiplierProperty);
            this.ClearValue(UseOptimizedHarvestingHealthProperty);
            this.ClearValue(ClampResourceHarvestDamageProperty);
            this.ClearValue(ClampItemSpoilingTimesProperty);

            this.ClearValue(BaseTemperatureMultiplierProperty);
            this.ClearValue(DayCycleSpeedScaleProperty);
            this.ClearValue(DayTimeSpeedScaleProperty);
            this.ClearValue(NightTimeSpeedScaleProperty);
            this.ClearValue(DisableWeatherFogProperty);

            this.ClearValue(GlobalSpoilingTimeMultiplierProperty);
            this.ClearValue(GlobalItemDecompositionTimeMultiplierProperty);
            this.ClearValue(GlobalCorpseDecompositionTimeMultiplierProperty);
            this.ClearValue(CropDecaySpeedMultiplierProperty);
            this.ClearValue(CropGrowthSpeedMultiplierProperty);
            this.ClearValue(LayEggIntervalMultiplierProperty);
            this.ClearValue(PoopIntervalMultiplierProperty);
            this.ClearValue(HairGrowthSpeedMultiplierProperty);

            this.ClearValue(CraftXPMultiplierProperty);
            this.ClearValue(GenericXPMultiplierProperty);
            this.ClearValue(HarvestXPMultiplierProperty);
            this.ClearValue(KillXPMultiplierProperty);
            this.ClearValue(SpecialXPMultiplierProperty);

            this.HarvestResourceItemAmountClassMultipliers = new ResourceClassMultiplierList(nameof(HarvestResourceItemAmountClassMultipliers), GameData.GetResourceMultipliers);
            this.HarvestResourceItemAmountClassMultipliers.Reset();
        }

        public void ResetHUDAndVisualsSection()
        {
            this.ClearValue(AllowCrosshairProperty);
            this.ClearValue(AllowHUDProperty);
            this.ClearValue(AllowThirdPersonViewProperty);
            this.ClearValue(AllowMapPlayerLocationProperty);
            this.ClearValue(AllowPVPGammaProperty);
            this.ClearValue(AllowPvEGammaProperty);
            this.ClearValue(ShowFloatingDamageTextProperty);
            this.ClearValue(AllowHitMarkersProperty);
        }

        public void ResetNPCSpawnOverridesSection()
        {
            this.ConfigAddNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(ConfigAddNPCSpawnEntriesContainer), NPCSpawnContainerType.Add);
            this.ConfigAddNPCSpawnEntriesContainer.Reset();

            this.ConfigSubtractNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(ConfigSubtractNPCSpawnEntriesContainer), NPCSpawnContainerType.Subtract);
            this.ConfigSubtractNPCSpawnEntriesContainer.Reset();

            this.ConfigOverrideNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(ConfigOverrideNPCSpawnEntriesContainer), NPCSpawnContainerType.Override);
            this.ConfigOverrideNPCSpawnEntriesContainer.Reset();

            this.NPCSpawnSettings = new NPCSpawnSettingsList(this.ConfigAddNPCSpawnEntriesContainer, this.ConfigSubtractNPCSpawnEntriesContainer, this.ConfigOverrideNPCSpawnEntriesContainer);
        }

        public void ResetPGMSection()
        {
            this.ClearValue(PGM_EnabledProperty);
            this.ClearValue(PGM_NameProperty);
            this.PGM_Terrain = new PGMTerrain();
        }

        public void ResetPlayerSettings()
        {
            this.ClearValue(EnableFlyerCarryProperty);
            this.ClearValue(XPMultiplierProperty);
            this.ClearValue(PlayerDamageMultiplierProperty);
            this.ClearValue(PlayerResistanceMultiplierProperty);
            this.ClearValue(PlayerCharacterWaterDrainMultiplierProperty);
            this.ClearValue(PlayerCharacterFoodDrainMultiplierProperty);
            this.ClearValue(PlayerCharacterStaminaDrainMultiplierProperty);
            this.ClearValue(PlayerCharacterHealthRecoveryMultiplierProperty);
            this.ClearValue(PlayerHarvestingDamageMultiplierProperty);
            this.ClearValue(CraftingSkillBonusMultiplierProperty);
            this.ClearValue(MaxFallSpeedMultiplierProperty);

            this.PlayerBaseStatMultipliers = new StatsMultiplierFloatArray(nameof(PlayerBaseStatMultipliers), GameData.GetBaseStatMultipliers_Player, GameData.GetStatMultiplierInclusions_PlayerBase(), true);
            this.PerLevelStatsMultiplier_Player = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_Player), GameData.GetPerLevelStatsMultipliers_Player, GameData.GetStatMultiplierInclusions_PlayerPerLevel(), true);
        }

        public void ResetPreventTransferOverridesSection()
        {
            this.PreventTransferForClassNames = new PreventTransferOverrideList(nameof(PreventTransferForClassNames));
            this.PreventTransferForClassNames.Reset();
        }

        public void ResetRulesSection()
        {
            this.ClearValue(EnableHardcoreProperty);
            this.ClearValue(EnablePVPProperty);
            this.ClearValue(EnableCreativeModeProperty);
            this.ClearValue(AllowCaveBuildingPvEProperty);
            this.ClearValue(DisableFriendlyFirePvPProperty);
            this.ClearValue(DisableFriendlyFirePvEProperty);
            this.ClearValue(AllowCaveBuildingPvPProperty);
            this.ClearValue(DisableRailgunPVPProperty);
            this.ClearValue(DisableLootCratesProperty);
            this.ClearValue(AllowCrateSpawnsOnTopOfStructuresProperty);
            this.ClearValue(EnableExtraStructurePreventionVolumesProperty);
            this.ClearValue(UseSingleplayerSettingsProperty);

            this.ClearValue(EnableDifficultyOverrideProperty);
            this.ClearValue(OverrideOfficialDifficultyProperty);
            this.ClearValue(DifficultyOffsetProperty);
            this.ClearValue(DestroyTamesOverLevelClamp﻿Property);

            this.ClearValue(EnableTributeDownloadsProperty);
            this.ClearValue(PreventDownloadSurvivorsProperty);
            this.ClearValue(PreventDownloadItemsProperty);
            this.ClearValue(PreventDownloadDinosProperty);
            this.ClearValue(PreventUploadSurvivorsProperty);
            this.ClearValue(PreventUploadItemsProperty);
            this.ClearValue(PreventUploadDinosProperty);
            this.ClearNullableValue(MaxTributeDinosProperty);
            this.ClearNullableValue(MaxTributeItemsProperty);

            this.ClearValue(NoTransferFromFilteringProperty);
            this.ClearValue(DisableCustomFoldersInTributeInventoriesProperty);
            this.ClearValue(OverrideTributeCharacterExpirationSecondsProperty);
            this.ClearValue(OverrideTributeItemExpirationSecondsProperty);
            this.ClearValue(OverrideTributeDinoExpirationSecondsProperty);
            this.ClearValue(OverrideMinimumDinoReuploadIntervalProperty);
            this.ClearValue(TributeCharacterExpirationSecondsProperty);
            this.ClearValue(TributeItemExpirationSecondsProperty);
            this.ClearValue(TributeDinoExpirationSecondsProperty);
            this.ClearValue(MinimumDinoReuploadIntervalProperty);
            this.ClearValue(CrossARKAllowForeignDinoDownloadsProperty);

            this.ClearValue(IncreasePvPRespawnIntervalProperty);
            this.ClearValue(IncreasePvPRespawnIntervalCheckPeriodProperty);
            this.ClearValue(IncreasePvPRespawnIntervalMultiplierProperty);
            this.ClearValue(IncreasePvPRespawnIntervalBaseAmountProperty);

            this.ClearValue(PreventOfflinePvPProperty);
            this.ClearValue(PreventOfflinePvPIntervalProperty);
            this.ClearValue(PreventOfflinePvPConnectionInvincibleIntervalProperty);

            this.ClearValue(AutoPvETimerProperty);
            this.ClearValue(AutoPvEUseSystemTimeProperty);
            this.ClearValue(AutoPvEStartTimeSecondsProperty);
            this.ClearValue(AutoPvEStopTimeSecondsProperty);

            this.ClearValue(MaxNumberOfPlayersInTribeProperty);
            this.ClearValue(TribeNameChangeCooldownProperty);
            this.ClearValue(TribeSlotReuseCooldownProperty);
            this.ClearValue(AllowTribeAlliancesProperty);
            this.ClearValue(MaxAlliancesPerTribeProperty);
            this.ClearValue(MaxTribesPerAllianceProperty);
            this.ClearValue(AllowTribeWarPvEProperty);
            this.ClearValue(AllowTribeWarCancelPvEProperty);

            this.ClearValue(AllowCustomRecipesProperty);
            this.ClearValue(CustomRecipeEffectivenessMultiplierProperty);
            this.ClearValue(CustomRecipeSkillMultiplierProperty);

            this.ClearValue(EnableDiseasesProperty);
            this.ClearValue(NonPermanentDiseasesProperty);

            this.ClearValue(OverrideNPCNetworkStasisRangeScaleProperty);
            this.ClearValue(NPCNetworkStasisRangeScalePlayerCountStartProperty);
            this.ClearValue(NPCNetworkStasisRangeScalePlayerCountEndProperty);
            this.ClearValue(NPCNetworkStasisRangeScalePercentEndProperty);

            this.ClearValue(UseCorpseLocatorProperty);
            this.ClearValue(PreventSpawnAnimationsProperty);
            this.ClearValue(AllowUnlimitedRespecsProperty);
            this.ClearValue(AllowPlatformSaddleMultiFloorsProperty);
            this.ClearValue(PlatformSaddleBuildAreaBoundsMultiplierProperty);
            this.ClearValue(MaxGateFrameOnSaddlesProperty);
            this.ClearValue(OxygenSwimSpeedStatMultiplierProperty);
            this.ClearValue(SupplyCrateLootQualityMultiplierProperty);
            this.ClearValue(FishingLootQualityMultiplierProperty);
            this.ClearValue(UseCorpseLifeSpanMultiplierProperty);
            this.ClearValue(MinimumTimeBetweenInventoryRetrievalProperty);
            this.ClearValue(GlobalPoweredBatteryDurabilityDecreasePerSecondProperty);
            this.ClearValue(RandomSupplyCratePointsProperty);
            this.ClearValue(FuelConsumptionIntervalMultiplierProperty);
            this.ClearValue(LimitNonPlayerDroppedItemsRangeProperty);
            this.ClearValue(LimitNonPlayerDroppedItemsCountProperty);

            this.ClearValue(EnableCryoSicknessPVEProperty);
            this.ClearValue(EnableCryopodNerfProperty);
            this.ClearValue(CryopodNerfDurationProperty);
            this.ClearValue(CryopodNerfDamageMultiplierProperty);
            this.ClearValue(CryopodNerfIncomingDamageMultiplierPercentProperty);

            this.ClearValue(AllowTekSuitPowersInGenesisProperty);
            this.ClearValue(DisableGenesisMissionsProperty);
            this.ClearValue(DisableDefaultMapItemSetsProperty);
            this.ClearValue(DisableWorldBuffsProperty);
            this.ClearValue(EnableWorldBuffScalingProperty);
            this.ClearValue(WorldBuffScalingEfficacyProperty);
            this.ClearValue(AdjustableMutagenSpawnDelayMultiplierProperty);

            this.ClearValue(MaxHexagonsPerCharacterProperty);
            this.ClearValue(DisableHexagonStoreProperty);
            this.ClearValue(HexStoreAllowOnlyEngramTradeOptionProperty);
            this.ClearValue(HexagonRewardMultiplierProperty);
            this.ClearValue(HexagonCostMultiplierProperty);

            this.ClearValue(Ragnarok_EnableSettingsProperty);
            this.ClearValue(Ragnarok_AllowMultipleTamedUnicornsProperty);
            this.ClearValue(Ragnarok_UnicornSpawnIntervalProperty);
            this.ClearValue(Ragnarok_EnableVolcanoProperty);
            this.ClearValue(Ragnarok_VolcanoIntervalProperty);
            this.ClearValue(Ragnarok_VolcanoIntensityProperty);

            this.ClearValue(Fjordur_EnableSettingsProperty);
            this.ClearValue(UseFjordurTraversalBuffProperty);

            this.ClearNullableValue(ItemStatClamps_GenericQualityProperty);
            this.ClearNullableValue(ItemStatClamps_ArmorProperty);
            this.ClearNullableValue(ItemStatClamps_MaxDurabilityProperty);
            this.ClearNullableValue(ItemStatClamps_WeaponDamagePercentProperty);
            this.ClearNullableValue(ItemStatClamps_WeaponClipAmmoProperty);
            this.ClearNullableValue(ItemStatClamps_HypothermalInsulationProperty);
            this.ClearNullableValue(ItemStatClamps_WeightProperty);
            this.ClearNullableValue(ItemStatClamps_HyperthermalInsulationProperty);
        }

        public void ResetServerDetailsSection()
        {
            this.ClearValue(BranchNameProperty);
            this.ClearValue(BranchPasswordProperty);

            this.ClearValue(EventNameProperty);
            this.ClearValue(EventColorsChanceOverrideProperty);
            this.ClearNullableValue(NewYear1UTCProperty);
            this.ClearNullableValue(NewYear2UTCProperty);
        }

        public void ResetSOTFSection()
        {
            this.ClearValue(SOTF_EnabledProperty);
            this.ClearValue(SOTF_DisableDeathSPectatorProperty);
            this.ClearValue(SOTF_OnlyAdminRejoinAsSpectatorProperty);
            this.ClearValue(SOTF_GamePlayLoggingProperty);
            this.ClearValue(SOTF_OutputGameReportProperty);
            this.ClearValue(SOTF_MaxNumberOfPlayersInTribeProperty);
            this.ClearValue(SOTF_BattleNumOfTribesToStartGameProperty);
            this.ClearValue(SOTF_TimeToCollapseRODProperty);
            this.ClearValue(SOTF_BattleAutoStartGameIntervalProperty);
            this.ClearValue(SOTF_BattleAutoRestartGameIntervalProperty);
            this.ClearValue(SOTF_BattleSuddenDeathIntervalProperty);

            this.ClearValue(SOTF_NoEventsProperty);
            this.ClearValue(SOTF_NoBossesProperty);
            this.ClearValue(SOTF_BothBossesProperty);
            this.ClearValue(SOTF_EvoEventIntervalProperty);
            this.ClearValue(SOTF_RingStartTimeProperty);
        }

        public void ResetStackSizeOverridesSection()
        {
            this.ClearValue(ItemStackSizeMultiplierProperty);

            this.ConfigOverrideItemMaxQuantity = new StackSizeOverrideList(nameof(ConfigOverrideItemMaxQuantity));
            this.ConfigOverrideItemMaxQuantity.Reset();
        }

        public void ResetStructuresSection()
        {
            this.ClearValue(StructureResistanceMultiplierProperty);
            this.ClearValue(StructureDamageMultiplierProperty);
            this.ClearValue(StructureDamageRepairCooldownProperty);
            this.ClearValue(PvPStructureDecayProperty);
            this.ClearValue(PvPZoneStructureDamageMultiplierProperty);
            this.ClearValue(MaxStructuresInRangeProperty);
            this.ClearValue(PerPlatformMaxStructuresMultiplierProperty);
            this.ClearValue(MaxPlatformSaddleStructureLimitProperty);
            this.ClearValue(OverrideStructurePlatformPreventionProperty);
            this.ClearValue(FlyerPlatformAllowUnalignedDinoBasingProperty);
            this.ClearValue(PvEAllowStructuresAtSupplyDropsProperty);
            this.ClearValue(EnableStructureDecayPvEProperty);
            this.ClearValue(PvEStructureDecayPeriodMultiplierProperty);
            this.ClearValue(AutoDestroyOldStructuresMultiplierProperty);
            this.ClearValue(ForceAllStructureLockingProperty);
            this.ClearValue(PassiveDefensesDamageRiderlessDinosProperty);
            this.ClearValue(EnableAutoDestroyStructuresProperty);
            this.ClearValue(OnlyAutoDestroyCoreStructuresProperty);
            this.ClearValue(OnlyDecayUnsnappedCoreStructuresProperty);
            this.ClearValue(FastDecayUnsnappedCoreStructuresProperty);
            this.ClearValue(DestroyUnconnectedWaterPipesProperty);
            this.ClearValue(DisableStructurePlacementCollisionProperty);
            this.ClearValue(IgnoreLimitMaxStructuresInRangeTypeFlagProperty);
            this.ClearValue(EnableFastDecayIntervalProperty);
            this.ClearValue(FastDecayIntervalProperty);
            this.ClearValue(LimitTurretsInRangeProperty);
            this.ClearValue(LimitTurretsRangeProperty);
            this.ClearValue(LimitTurretsNumProperty);
            this.ClearValue(HardLimitTurretsInRangeProperty);
            this.ClearValue(AlwaysAllowStructurePickupProperty);
            this.ClearValue(StructurePickupTimeAfterPlacementProperty);
            this.ClearValue(StructurePickupHoldDurationProperty);
            this.ClearValue(AllowIntegratedSPlusStructuresProperty);
            this.ClearValue(IgnoreStructuresPreventionVolumesProperty);
            this.ClearValue(GenesisUseStructuresPreventionVolumesProperty);
        }

        public void ResetSupplyCrateOverridesSection()
        {
            this.ConfigOverrideSupplyCrateItems = new SupplyCrateOverrideList(nameof(ConfigOverrideSupplyCrateItems));
            this.ConfigOverrideSupplyCrateItems.Reset();
        }

        public void ResetExcludeItemIndicesOverridesSection()
        {
            this.ExcludeItemIndices = new ExcludeItemIndicesOverrideList(nameof(ExcludeItemIndices));
            this.ExcludeItemIndices.Reset();
        }

        public void UpdateOverrideMaxExperiencePointsDino()
        {
            if (EnableLevelProgressions && EnableDinoLevelProgressions)
            {
                var list = GetLevelList(LevelProgression.Dino);
                if (list != null && list.Count > 0)
                {
                    OverrideMaxExperiencePointsDino.SetValue(list[list.Count - 1].XPRequired);
                    return;
                }
            }

            ResetOverrideMaxExperiencePointsDino();
        }

        public void UpdateOverrideMaxExperiencePointsPlayer()
        {
            if (EnableLevelProgressions)
            {
                var list = GetLevelList(LevelProgression.Player);
                if (list != null && list.Count > 0)
                {
                    OverrideMaxExperiencePointsPlayer.SetValue(list[list.Count - 1].XPRequired);
                    return;
                }
            }

            ResetOverrideMaxExperiencePointsPlayer();
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
                case ServerProfileCategory.Rules:
                    SyncRulesSection(sourceProfile);
                    break;
                case ServerProfileCategory.ChatAndNotifications:
                    SyncChatAndNotificationsSection(sourceProfile);
                    break;
                case ServerProfileCategory.HudAndVisuals:
                    SyncHudAndVisualsSection(sourceProfile);
                    break;
                case ServerProfileCategory.Players:
                    SyncPlayerSettingsSection(sourceProfile);
                    break;
                case ServerProfileCategory.Dinos:
                    SyncDinoSettingsSection(sourceProfile);
                    break;
                case ServerProfileCategory.Environment:
                    SyncEnvironmentSection(sourceProfile);
                    break;
                case ServerProfileCategory.Structures:
                    SyncStructuresSection(sourceProfile);
                    break;
                case ServerProfileCategory.Engrams:
                    SyncEngramsSection(sourceProfile);
                    break;
                case ServerProfileCategory.ServerFiles:
                    SyncServerFiles(sourceProfile);
                    break;
                case ServerProfileCategory.CustomEngineSettings:
                    SyncCustomEngineSettingsSection(sourceProfile);
                    break;
                case ServerProfileCategory.CustomGameSettings:
                    SyncCustomGameSettingsSection(sourceProfile);
                    break;
                case ServerProfileCategory.CustomGameUserSettings:
                    SyncCustomGameUserSettingsSection(sourceProfile);
                    break;
                case ServerProfileCategory.CustomLevels:
                    SyncCustomLevelsSection(sourceProfile);
                    break;
                case ServerProfileCategory.MapSpawnerOverrides:
                    SyncNPCSpawnOverridesSection(sourceProfile);
                    break;
                case ServerProfileCategory.CraftingOverrides:
                    SyncCraftingOverridesSection(sourceProfile);
                    break;
                case ServerProfileCategory.SupplyCrateOverrides:
                    SyncSupplyCrateOverridesSection(sourceProfile);
                    break;
                case ServerProfileCategory.ExcludeItemIndicesOverrides:
                    SyncExcludeItemIndicesOverridesSection(sourceProfile);
                    break;
                case ServerProfileCategory.StackSizeOverrides:
                    SyncStackSizeOverridesSection(sourceProfile);
                    break;
                case ServerProfileCategory.PreventTransferOverrides:
                    SyncPreventTransferOverridesSection(sourceProfile);
                    break;
                case ServerProfileCategory.PGM:
                    SyncPGMSection(sourceProfile);
                    break;
                case ServerProfileCategory.SOTF:
                    SyncSOTFSection(sourceProfile);
                    break;
            }
        }

        private void SyncAdministrationSection(ServerProfile sourceProfile)
        {
            if (Config.Default.ProfileSyncServerModIdsEnabled)
            {
                this.SetValue(ServerModIdsProperty, sourceProfile.ServerModIds);
            }

            this.SetValue(AutoSavePeriodMinutesProperty, sourceProfile.AutoSavePeriodMinutes);
            this.SetValue(MaxNumOfSaveBackupsProperty, sourceProfile.MaxNumOfSaveBackups);

            this.SetValue(EnableExtinctionEventProperty, sourceProfile.EnableExtinctionEvent);
            this.SetValue(ExtinctionEventTimeIntervalProperty, sourceProfile.ExtinctionEventTimeInterval);
            this.SetValue(ExtinctionEventUTCProperty, sourceProfile.ExtinctionEventUTC);

            // server options
            this.SetValue(CultureProperty, sourceProfile.Culture);
            this.SetValue(MaxPlayersProperty, sourceProfile.MaxPlayers);
            this.SetNullableValue(KickIdlePlayersPeriodProperty, sourceProfile.KickIdlePlayersPeriod);
            this.SetValue(EnableBanListURLProperty, sourceProfile.EnableBanListURL);
            this.SetValue(BanListURLProperty, sourceProfile.BanListURL);
            this.SetValue(EnableCustomDynamicConfigUrlProperty, sourceProfile.EnableCustomDynamicConfigUrl);
            this.SetValue(CustomDynamicConfigUrlProperty, sourceProfile.CustomDynamicConfigUrl);
            this.SetValue(EnableCustomLiveTuningUrlProperty, sourceProfile.EnableCustomLiveTuningUrl);
            this.SetValue(CustomLiveTuningUrlProperty, sourceProfile.CustomLiveTuningUrl);
            this.SetValue(DisableValveAntiCheatSystemProperty, sourceProfile.DisableValveAntiCheatSystem);
            this.SetValue(UseBattlEyeProperty, sourceProfile.UseBattlEye);
            this.SetValue(DisablePlayerMovePhysicsOptimizationProperty, sourceProfile.DisablePlayerMovePhysicsOptimization);
            this.SetValue(DisableAntiSpeedHackDetectionProperty, sourceProfile.DisableAntiSpeedHackDetection);
            this.SetValue(SpeedHackBiasProperty, sourceProfile.SpeedHackBias);
            this.SetValue(UseNoHangDetectionProperty, sourceProfile.UseNoHangDetection);
            this.SetValue(NoDinosProperty, sourceProfile.NoDinos);
            this.SetValue(NoUnderMeshCheckingProperty, sourceProfile.NoUnderMeshChecking);
            this.SetValue(NoUnderMeshKillingProperty, sourceProfile.NoUnderMeshKilling);
            this.SetValue(UseVivoxProperty, sourceProfile.UseVivox);
            this.SetValue(AllowSharedConnectionsProperty, sourceProfile.AllowSharedConnections);
            this.SetValue(ForceRespawnDinosProperty, sourceProfile.ForceRespawnDinos);
            this.SetValue(EnableServerAutoForceRespawnWildDinosIntervalProperty, sourceProfile.EnableServerAutoForceRespawnWildDinosInterval);
            this.SetValue(ServerAutoForceRespawnWildDinosIntervalProperty, sourceProfile.ServerAutoForceRespawnWildDinosInterval);
            this.SetValue(ForceDirectX10Property, sourceProfile.ForceDirectX10);
            this.SetValue(ForceShaderModel4Property, sourceProfile.ForceShaderModel4);
            this.SetValue(ForceLowMemoryProperty, sourceProfile.ForceLowMemory);
            this.SetValue(ForceNoManSkyProperty, sourceProfile.ForceNoManSky);
            this.SetValue(UseNoMemoryBiasProperty, sourceProfile.UseNoMemoryBias);
            this.SetValue(StasisKeepControllersProperty, sourceProfile.StasisKeepControllers);
            this.SetValue(ServerAllowAnselProperty, sourceProfile.ServerAllowAnsel);
            this.SetValue(StructureMemoryOptimizationsProperty, sourceProfile.StructureMemoryOptimizations);
            this.SetValue(UseStructureStasisGridProperty, sourceProfile.UseStructureStasisGrid);
            this.SetValue(CrossplayProperty, sourceProfile.Crossplay);
            this.SetValue(EpicOnlyProperty, sourceProfile.EpicOnly);
            this.SetValue(EnablePublicIPForEpicProperty, sourceProfile.EnablePublicIPForEpic);
            this.SetValue(OutputServerLogProperty, sourceProfile.OutputServerLog);
            //this.SetValue(AltSaveDirectoryNameProperty, sourceProfile.AltSaveDirectoryName);
            if (Config.Default.ProfileSyncCrossArkClusterIdEnabled)
            {
                this.SetValue(CrossArkClusterIdProperty, sourceProfile.CrossArkClusterId);
                this.SetValue(ClusterDirOverrideProperty, sourceProfile.ClusterDirOverride);
            }
            this.SetValue(ClusterDirOverrideProperty, sourceProfile.ClusterDirOverride);
            this.SetValue(SecureSendArKPayloadProperty, sourceProfile.SecureSendArKPayload);
            this.SetValue(UseItemDupeCheckProperty, sourceProfile.UseItemDupeCheck);
            this.SetValue(UseSecureSpawnRulesProperty, sourceProfile.UseSecureSpawnRules);

            // server filter options
            this.SetValue(EnableBadWordListURLProperty, sourceProfile.EnableBadWordListURL);
            this.SetValue(EnableBadWordWhiteListURLProperty, sourceProfile.EnableBadWordWhiteListURL);
            this.SetValue(BadWordListURLProperty, sourceProfile.BadWordListURL);
            this.SetValue(BadWordWhiteListURLProperty, sourceProfile.BadWordWhiteListURL);
            this.SetValue(FilterTribeNamesProperty, sourceProfile.FilterTribeNames);
            this.SetValue(FilterCharacterNamesProperty, sourceProfile.FilterCharacterNames);
            this.SetValue(FilterChatProperty, sourceProfile.FilterChat);

            // server log options
            this.SetValue(EnableServerAdminLogsProperty, sourceProfile.EnableServerAdminLogs);
            this.SetValue(ServerAdminLogsIncludeTribeLogsProperty, sourceProfile.ServerAdminLogsIncludeTribeLogs);
            this.SetValue(ServerRCONOutputTribeLogsProperty, sourceProfile.ServerRCONOutputTribeLogs);
            this.SetValue(AllowHideDamageSourceFromLogsProperty, sourceProfile.AllowHideDamageSourceFromLogs);
            this.SetValue(MaxTribeLogsProperty, sourceProfile.MaxTribeLogs);
            this.SetValue(NotifyAdminCommandsInChatProperty, sourceProfile.NotifyAdminCommandsInChat);
            this.SetValue(TribeLogDestroyedEnemyStructuresProperty, sourceProfile.TribeLogDestroyedEnemyStructures);
            
            this.SetValue(EnableWebAlarmProperty, sourceProfile.EnableWebAlarm);
            this.SetValue(WebAlarmKeyProperty, sourceProfile.WebAlarmKey);
            this.SetValue(WebAlarmUrlProperty, sourceProfile.WebAlarmUrl);

            //this.SetValue(LauncherArgsProperty, sourceProfile.LauncherArgs);
            //this.SetValue(LauncherArgsOverrideProperty, sourceProfile.LauncherArgsOverride);
            //this.SetValue(LauncherArgsPrefixProperty, sourceProfile.LauncherArgsPrefix);
            //this.SetValue(AdditionalArgsProperty, sourceProfile.AdditionalArgs);

            this.SetValue(NewSaveFormatProperty, sourceProfile.NewSaveFormat);
            this.SetValue(UseStoreProperty, sourceProfile.UseStore);
            this.SetValue(BackupTransferPlayerDatasProperty, sourceProfile.BackupTransferPlayerDatas);
        }

        private void SyncAutomaticManagement(ServerProfile sourceProfile)
        {
            this.SetValue(EnableAutoStartProperty, sourceProfile.EnableAutoStart);
            this.SetValue(AutoStartOnLoginProperty, sourceProfile.AutoStartOnLogin);

            this.SetValue(EnableAutoShutdown1Property, sourceProfile.EnableAutoShutdown1);
            if (Config.Default.ProfileSyncAutoShutdownEnabled)
            {
                this.SetValue(AutoShutdownTime1Property, sourceProfile.AutoShutdownTime1);
                this.SetValue(ShutdownDaysOfTheWeek1Property, sourceProfile.ShutdownDaysOfTheWeek1);
            }
            this.SetValue(RestartAfterShutdown1Property, sourceProfile.RestartAfterShutdown1);
            this.SetValue(UpdateAfterShutdown1Property, sourceProfile.UpdateAfterShutdown1);

            this.SetValue(EnableAutoShutdown2Property, sourceProfile.EnableAutoShutdown2);
            if (Config.Default.ProfileSyncAutoShutdownEnabled)
            {
                this.SetValue(AutoShutdownTime2Property, sourceProfile.AutoShutdownTime2);
                this.SetValue(ShutdownDaysOfTheWeek2Property, sourceProfile.ShutdownDaysOfTheWeek2);
            }
            this.SetValue(RestartAfterShutdown2Property, sourceProfile.RestartAfterShutdown2);
            this.SetValue(UpdateAfterShutdown2Property, sourceProfile.UpdateAfterShutdown2);

            this.SetValue(EnableAutoBackupProperty, sourceProfile.EnableAutoBackup);
            this.SetValue(EnableAutoUpdateProperty, sourceProfile.EnableAutoUpdate);
            this.SetValue(AutoRestartIfShutdownProperty, sourceProfile.AutoRestartIfShutdown);
        }

        private void SyncChatAndNotificationsSection(ServerProfile sourceProfile)
        {
            this.SetValue(EnableGlobalVoiceChatProperty, sourceProfile.EnableGlobalVoiceChat);
            this.SetValue(EnableProximityChatProperty, sourceProfile.EnableProximityChat);
            this.SetValue(EnablePlayerLeaveNotificationsProperty, sourceProfile.EnablePlayerLeaveNotifications);
            this.SetValue(EnablePlayerJoinedNotificationsProperty, sourceProfile.EnablePlayerJoinedNotifications);
        }

        private void SyncCraftingOverridesSection(ServerProfile sourceProfile)
        {
            sourceProfile.ConfigOverrideItemCraftingCosts.RenderToModel();

            this.ConfigOverrideItemCraftingCosts.Clear();
            this.ConfigOverrideItemCraftingCosts.FromIniValues(sourceProfile.ConfigOverrideItemCraftingCosts.ToIniValues());
            this.ConfigOverrideItemCraftingCosts.IsEnabled = this.ConfigOverrideItemCraftingCosts.Count > 0;
            this.ConfigOverrideItemCraftingCosts.RenderToView();
        }

        private void SyncCustomEngineSettingsSection(ServerProfile sourceProfile)
        {
            this.CustomEngineSettings.Clear();
            foreach (var section in sourceProfile.CustomEngineSettings)
            {
                this.CustomEngineSettings.Add(section.SectionName, section.ToIniValues());
            }
        }

        private void SyncCustomGameSettingsSection(ServerProfile sourceProfile)
        {
            this.CustomGameSettings.Clear();
            foreach (var section in sourceProfile.CustomGameSettings)
            {
                this.CustomGameSettings.Add(section.SectionName, section.ToIniValues());
            }
        }

        private void SyncCustomGameUserSettingsSection(ServerProfile sourceProfile)
        {
            this.CustomGameUserSettings.Clear();
            foreach (var section in sourceProfile.CustomGameUserSettings)
            {
                this.CustomGameUserSettings.Add(section.SectionName, section.ToIniValues());
            }
        }

        private void SyncCustomLevelsSection(ServerProfile sourceProfile)
        {
            this.SetNullableValue(OverrideMaxExperiencePointsPlayerProperty, sourceProfile.OverrideMaxExperiencePointsPlayer);
            this.SetNullableValue(OverrideMaxExperiencePointsDinoProperty, sourceProfile.OverrideMaxExperiencePointsDino);

            this.SetValue(EnableLevelProgressionsProperty, sourceProfile.EnableLevelProgressions);
            this.SetValue(EnableDinoLevelProgressionsProperty, sourceProfile.EnableDinoLevelProgressions);

            this.PlayerLevels = LevelList.FromINIValues(sourceProfile.PlayerLevels.ToINIValueForXP(), sourceProfile.PlayerLevels.ToINIValuesForEngramPoints());
            this.DinoLevels = LevelList.FromINIValues(sourceProfile.DinoLevels.ToINIValueForXP(), sourceProfile.DinoLevels.ToINIValuesForEngramPoints());
        }

        private void SyncDinoSettingsSection(ServerProfile sourceProfile)
        {
            this.SetValue(DinoDamageMultiplierProperty, sourceProfile.DinoDamageMultiplier);
            this.SetValue(TamedDinoDamageMultiplierProperty, sourceProfile.TamedDinoDamageMultiplier);
            this.SetValue(DinoResistanceMultiplierProperty, sourceProfile.DinoResistanceMultiplier);
            this.SetValue(TamedDinoResistanceMultiplierProperty, sourceProfile.TamedDinoResistanceMultiplier);
            this.SetValue(DinoCharacterFoodDrainMultiplierProperty, sourceProfile.DinoCharacterFoodDrainMultiplier);
            this.SetValue(DinoCharacterStaminaDrainMultiplierProperty, sourceProfile.DinoCharacterStaminaDrainMultiplier);
            this.SetValue(DinoCharacterHealthRecoveryMultiplierProperty, sourceProfile.DinoCharacterHealthRecoveryMultiplier);
            this.SetValue(DinoHarvestingDamageMultiplierProperty, sourceProfile.DinoHarvestingDamageMultiplier);
            this.SetValue(DinoTurretDamageMultiplierProperty, sourceProfile.DinoTurretDamageMultiplier);

            this.SetValue(AllowRaidDinoFeedingProperty, sourceProfile.AllowRaidDinoFeeding);
            this.SetValue(RaidDinoCharacterFoodDrainMultiplierProperty, sourceProfile.RaidDinoCharacterFoodDrainMultiplier);

            this.SetValue(EnableAllowCaveFlyersProperty, sourceProfile.EnableAllowCaveFlyers);
            this.SetValue(AllowFlyingStaminaRecoveryProperty, sourceProfile.AllowFlyingStaminaRecovery);
            this.SetValue(AllowFlyerSpeedLevelingProperty, sourceProfile.AllowFlyerSpeedLeveling);

            this.SetValue(PreventMateBoostProperty, sourceProfile.PreventMateBoost);
            this.SetValue(DisableDinoDecayPvEProperty, sourceProfile.DisableDinoDecayPvE);
            this.SetValue(DisableDinoDecayPvPProperty, sourceProfile.DisableDinoDecayPvP);
            this.SetValue(AutoDestroyDecayedDinosProperty, sourceProfile.AutoDestroyDecayedDinos);
            this.SetValue(UseDinoLevelUpAnimationsProperty, sourceProfile.UseDinoLevelUpAnimations);
            this.SetValue(PvEDinoDecayPeriodMultiplierProperty, sourceProfile.PvEDinoDecayPeriodMultiplier);
            this.SetValue(AllowMultipleAttachedC4Property, sourceProfile.AllowMultipleAttachedC4);
            this.SetValue(AllowUnclaimDinosProperty, sourceProfile.AllowUnclaimDinos);

            this.SetValue(DisableDinoRidingProperty, sourceProfile.DisableDinoRiding);
            this.SetValue(DisableDinoTamingProperty, sourceProfile.DisableDinoTaming);
            this.SetValue(DisableDinoBreedingProperty, sourceProfile.DisableDinoBreeding);
            this.SetValue(MaxTamedDinosProperty, sourceProfile.MaxTamedDinos);
            this.SetValue(MaxPersonalTamedDinosProperty, sourceProfile.MaxPersonalTamedDinos);
            this.SetValue(PersonalTamedDinosSaddleStructureCostProperty, sourceProfile.PersonalTamedDinosSaddleStructureCost);
            this.SetValue(UseTameLimitForStructuresOnlyProperty, sourceProfile.UseTameLimitForStructuresOnly);
            this.SetValue(EnableForceCanRideFliersProperty, sourceProfile.EnableForceCanRideFliers);
            this.SetValue(ForceCanRideFliersProperty, sourceProfile.ForceCanRideFliers);

            this.SetValue(MatingIntervalMultiplierProperty, sourceProfile.MatingIntervalMultiplier);
            this.SetValue(MatingSpeedMultiplierProperty, sourceProfile.MatingSpeedMultiplier);
            this.SetValue(EggHatchSpeedMultiplierProperty, sourceProfile.EggHatchSpeedMultiplier);
            this.SetValue(BabyMatureSpeedMultiplierProperty, sourceProfile.BabyMatureSpeedMultiplier);
            this.SetValue(BabyFoodConsumptionSpeedMultiplierProperty, sourceProfile.BabyFoodConsumptionSpeedMultiplier);

            this.SetValue(DisableImprintDinoBuffProperty, sourceProfile.DisableImprintDinoBuff);
            this.SetValue(AllowAnyoneBabyImprintCuddleProperty, sourceProfile.AllowAnyoneBabyImprintCuddle);
            this.SetValue(BabyImprintingStatScaleMultiplierProperty, sourceProfile.BabyImprintingStatScaleMultiplier);
            this.SetValue(BabyImprintAmountMultiplierProperty, sourceProfile.BabyImprintAmountMultiplier);
            this.SetValue(BabyCuddleIntervalMultiplierProperty, sourceProfile.BabyCuddleIntervalMultiplier);
            this.SetValue(BabyCuddleGracePeriodMultiplierProperty, sourceProfile.BabyCuddleGracePeriodMultiplier);
            this.SetValue(BabyCuddleLoseImprintQualitySpeedMultiplierProperty, sourceProfile.BabyCuddleLoseImprintQualitySpeedMultiplier);
            this.SetNullableValue(ImprintlimitProperty, sourceProfile.Imprintlimit);

            this.SetValue(WildDinoCharacterFoodDrainMultiplierProperty, sourceProfile.WildDinoCharacterFoodDrainMultiplier);
            this.SetValue(TamedDinoCharacterFoodDrainMultiplierProperty, sourceProfile.TamedDinoCharacterFoodDrainMultiplier);
            this.SetValue(WildDinoTorporDrainMultiplierProperty, sourceProfile.WildDinoTorporDrainMultiplier);
            this.SetValue(TamedDinoTorporDrainMultiplierProperty, sourceProfile.TamedDinoTorporDrainMultiplier);
            this.SetValue(PassiveTameIntervalMultiplierProperty, sourceProfile.PassiveTameIntervalMultiplier);

            this.PerLevelStatsMultiplier_DinoWild = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoWild), GameData.GetPerLevelStatsMultipliers_DinoWild, GameData.GetStatMultiplierInclusions_DinoWildPerLevel(), true);
            this.PerLevelStatsMultiplier_DinoWild.FromIniValues(sourceProfile.PerLevelStatsMultiplier_DinoWild.ToIniValues());
            this.PerLevelStatsMultiplier_DinoWild.IsEnabled = sourceProfile.PerLevelStatsMultiplier_DinoWild.IsEnabled;

            this.PerLevelStatsMultiplier_DinoTamed = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoTamed), GameData.GetPerLevelStatsMultipliers_DinoTamed, GameData.GetStatMultiplierInclusions_DinoTamedPerLevel(), true);
            this.PerLevelStatsMultiplier_DinoTamed.FromIniValues(sourceProfile.PerLevelStatsMultiplier_DinoTamed.ToIniValues());
            this.PerLevelStatsMultiplier_DinoTamed.IsEnabled = sourceProfile.PerLevelStatsMultiplier_DinoTamed.IsEnabled;

            this.PerLevelStatsMultiplier_DinoTamed_Add = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoTamed_Add), GameData.GetPerLevelStatsMultipliers_DinoTamedAdd, GameData.GetStatMultiplierInclusions_DinoTamedAdd(), true);
            this.PerLevelStatsMultiplier_DinoTamed_Add.FromIniValues(sourceProfile.PerLevelStatsMultiplier_DinoTamed_Add.ToIniValues());
            this.PerLevelStatsMultiplier_DinoTamed_Add.IsEnabled = sourceProfile.PerLevelStatsMultiplier_DinoTamed_Add.IsEnabled;

            this.PerLevelStatsMultiplier_DinoTamed_Affinity = new StatsMultiplierFloatArray(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity), GameData.GetPerLevelStatsMultipliers_DinoTamedAffinity, GameData.GetStatMultiplierInclusions_DinoTamedAffinity(), true);
            this.PerLevelStatsMultiplier_DinoTamed_Affinity.FromIniValues(sourceProfile.PerLevelStatsMultiplier_DinoTamed_Affinity.ToIniValues());
            this.PerLevelStatsMultiplier_DinoTamed_Affinity.IsEnabled = sourceProfile.PerLevelStatsMultiplier_DinoTamed_Affinity.IsEnabled;

            this.MutagenLevelBoost = new StatsMultiplierIntegerArray(nameof(MutagenLevelBoost), GameData.GetPerLevelMutagenLevelBoost_DinoWild, GameData.GetMutagenLevelBoostInclusions_DinoWild(), true);
            this.MutagenLevelBoost.FromIniValues(sourceProfile.MutagenLevelBoost.ToIniValues());
            this.MutagenLevelBoost.IsEnabled = sourceProfile.MutagenLevelBoost.IsEnabled;

            this.MutagenLevelBoost_Bred = new StatsMultiplierIntegerArray(nameof(MutagenLevelBoost_Bred), GameData.GetPerLevelMutagenLevelBoost_DinoTamed, GameData.GetMutagenLevelBoostInclusions_DinoTamed(), true);
            this.MutagenLevelBoost_Bred.FromIniValues(sourceProfile.MutagenLevelBoost_Bred.ToIniValues());
            this.MutagenLevelBoost_Bred.IsEnabled = sourceProfile.MutagenLevelBoost_Bred.IsEnabled;

            sourceProfile.DinoSettings.RenderToModel();

            this.DinoSpawnWeightMultipliers.Clear();
            this.DinoSpawnWeightMultipliers.FromIniValues(sourceProfile.DinoSpawnWeightMultipliers.ToIniValues());
            this.DinoSpawnWeightMultipliers.IsEnabled = sourceProfile.DinoSpawnWeightMultipliers.IsEnabled;

            this.PreventDinoTameClassNames.Clear();
            this.PreventDinoTameClassNames.FromIniValues(sourceProfile.PreventDinoTameClassNames.ToIniValues());
            this.PreventDinoTameClassNames.IsEnabled = sourceProfile.PreventDinoTameClassNames.IsEnabled;

            this.PreventBreedingForClassNames.Clear();
            this.PreventBreedingForClassNames.FromIniValues(sourceProfile.PreventBreedingForClassNames.ToIniValues());
            this.PreventBreedingForClassNames.IsEnabled = sourceProfile.PreventBreedingForClassNames.IsEnabled;

            this.NPCReplacements.Clear();
            this.NPCReplacements.FromIniValues(sourceProfile.NPCReplacements.ToIniValues());
            this.NPCReplacements.IsEnabled = sourceProfile.NPCReplacements.IsEnabled;

            this.TamedDinoClassDamageMultipliers.Clear();
            this.TamedDinoClassDamageMultipliers.FromIniValues(sourceProfile.TamedDinoClassDamageMultipliers.ToIniValues());
            this.TamedDinoClassDamageMultipliers.IsEnabled = sourceProfile.TamedDinoClassDamageMultipliers.IsEnabled;

            this.TamedDinoClassResistanceMultipliers.Clear();
            this.TamedDinoClassResistanceMultipliers.FromIniValues(sourceProfile.TamedDinoClassResistanceMultipliers.ToIniValues());
            this.TamedDinoClassResistanceMultipliers.IsEnabled = sourceProfile.TamedDinoClassResistanceMultipliers.IsEnabled;

            this.DinoClassDamageMultipliers.Clear();
            this.DinoClassDamageMultipliers.FromIniValues(sourceProfile.DinoClassDamageMultipliers.ToIniValues());
            this.DinoClassDamageMultipliers.IsEnabled = sourceProfile.DinoClassDamageMultipliers.IsEnabled;

            this.DinoClassResistanceMultipliers.Clear();
            this.DinoClassResistanceMultipliers.FromIniValues(sourceProfile.DinoClassResistanceMultipliers.ToIniValues());
            this.DinoClassResistanceMultipliers.IsEnabled = sourceProfile.DinoClassResistanceMultipliers.IsEnabled;

            this.DinoSettings = new DinoSettingsList(this.DinoSpawnWeightMultipliers, this.PreventDinoTameClassNames, this.PreventBreedingForClassNames, this.NPCReplacements, this.TamedDinoClassDamageMultipliers, this.TamedDinoClassResistanceMultipliers, this.DinoClassDamageMultipliers, this.DinoClassResistanceMultipliers);
            this.DinoSettings.RenderToView();
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

        private void SyncEngramsSection(ServerProfile sourceProfile)
        {
            this.SetValue(AutoUnlockAllEngramsProperty, sourceProfile.AutoUnlockAllEngrams);
            this.SetValue(OnlyAllowSpecifiedEngramsProperty, sourceProfile.OnlyAllowSpecifiedEngrams);

            sourceProfile.EngramSettings.RenderToModel();

            this.OverrideNamedEngramEntries.Clear();
            this.OverrideNamedEngramEntries.FromIniValues(sourceProfile.OverrideNamedEngramEntries.ToIniValues());
            this.OverrideNamedEngramEntries.IsEnabled = this.OverrideNamedEngramEntries.Count > 0;

            this.EngramEntryAutoUnlocks.Clear();
            this.EngramEntryAutoUnlocks.FromIniValues(sourceProfile.EngramEntryAutoUnlocks.ToIniValues());
            this.EngramEntryAutoUnlocks.IsEnabled = this.EngramEntryAutoUnlocks.Count > 0;

            this.EngramSettings = new EngramSettingsList(this.OverrideNamedEngramEntries, this.EngramEntryAutoUnlocks)
            {
                OnlyAllowSpecifiedEngrams = sourceProfile.OnlyAllowSpecifiedEngrams
            };
            this.EngramSettings.RenderToView();
        }

        private void SyncEnvironmentSection(ServerProfile sourceProfile)
        {
            this.SetValue(DinoCountMultiplierProperty, sourceProfile.DinoCountMultiplier);
            this.SetValue(TamingSpeedMultiplierProperty, sourceProfile.TamingSpeedMultiplier);
            this.SetValue(HarvestAmountMultiplierProperty, sourceProfile.HarvestAmountMultiplier);
            this.SetValue(ResourcesRespawnPeriodMultiplierProperty, sourceProfile.ResourcesRespawnPeriodMultiplier);
            this.SetValue(ResourceNoReplenishRadiusPlayersProperty, sourceProfile.ResourceNoReplenishRadiusPlayers);
            this.SetValue(ResourceNoReplenishRadiusStructuresProperty, sourceProfile.ResourceNoReplenishRadiusStructures);
            this.SetValue(HarvestHealthMultiplierProperty, sourceProfile.HarvestHealthMultiplier);
            this.SetValue(UseOptimizedHarvestingHealthProperty, sourceProfile.UseOptimizedHarvestingHealth);
            this.SetValue(ClampResourceHarvestDamageProperty, sourceProfile.ClampResourceHarvestDamage);
            this.SetValue(ClampItemSpoilingTimesProperty, sourceProfile.ClampItemSpoilingTimes);
            
            this.SetValue(BaseTemperatureMultiplierProperty, sourceProfile.BaseTemperatureMultiplier);
            this.SetValue(DayCycleSpeedScaleProperty, sourceProfile.DayCycleSpeedScale);
            this.SetValue(DayTimeSpeedScaleProperty, sourceProfile.DayTimeSpeedScale);
            this.SetValue(NightTimeSpeedScaleProperty, sourceProfile.NightTimeSpeedScale);
            this.SetValue(DisableWeatherFogProperty, sourceProfile.DisableWeatherFog);

            this.SetValue(GlobalSpoilingTimeMultiplierProperty, sourceProfile.GlobalSpoilingTimeMultiplier);
            this.SetValue(GlobalItemDecompositionTimeMultiplierProperty, sourceProfile.GlobalItemDecompositionTimeMultiplier);
            this.SetValue(GlobalCorpseDecompositionTimeMultiplierProperty, sourceProfile.GlobalCorpseDecompositionTimeMultiplier);
            this.SetValue(CropDecaySpeedMultiplierProperty, sourceProfile.CropDecaySpeedMultiplier);
            this.SetValue(CropGrowthSpeedMultiplierProperty, sourceProfile.CropGrowthSpeedMultiplier);
            this.SetValue(LayEggIntervalMultiplierProperty, sourceProfile.LayEggIntervalMultiplier);
            this.SetValue(PoopIntervalMultiplierProperty, sourceProfile.PoopIntervalMultiplier);
            this.SetValue(HairGrowthSpeedMultiplierProperty, sourceProfile.HairGrowthSpeedMultiplier);

            this.SetValue(CraftXPMultiplierProperty, sourceProfile.CraftXPMultiplier);
            this.SetValue(GenericXPMultiplierProperty, sourceProfile.GenericXPMultiplier);
            this.SetValue(HarvestXPMultiplierProperty, sourceProfile.HarvestXPMultiplier);
            this.SetValue(KillXPMultiplierProperty, sourceProfile.KillXPMultiplier);
            this.SetValue(SpecialXPMultiplierProperty, sourceProfile.SpecialXPMultiplier);

            this.HarvestResourceItemAmountClassMultipliers.Clear();
            this.HarvestResourceItemAmountClassMultipliers.FromIniValues(sourceProfile.HarvestResourceItemAmountClassMultipliers.ToIniValues());
            this.HarvestResourceItemAmountClassMultipliers.IsEnabled = sourceProfile.HarvestResourceItemAmountClassMultipliers.IsEnabled;
        }

        private void SyncHudAndVisualsSection(ServerProfile sourceProfile)
        {
            this.SetValue(AllowCrosshairProperty, sourceProfile.AllowCrosshair);
            this.SetValue(AllowHUDProperty, sourceProfile.AllowHUD);
            this.SetValue(AllowThirdPersonViewProperty, sourceProfile.AllowThirdPersonView);
            this.SetValue(AllowMapPlayerLocationProperty, sourceProfile.AllowMapPlayerLocation);
            this.SetValue(AllowPVPGammaProperty, sourceProfile.AllowPVPGamma);
            this.SetValue(AllowPvEGammaProperty, sourceProfile.AllowPvEGamma);
            this.SetValue(ShowFloatingDamageTextProperty, sourceProfile.ShowFloatingDamageText);
            this.SetValue(AllowHitMarkersProperty, sourceProfile.AllowHitMarkers);
        }

        private void SyncNPCSpawnOverridesSection(ServerProfile sourceProfile)
        {
            sourceProfile.NPCSpawnSettings.RenderToModel();

            this.ConfigAddNPCSpawnEntriesContainer.Clear();
            this.ConfigAddNPCSpawnEntriesContainer.FromIniValues(sourceProfile.ConfigAddNPCSpawnEntriesContainer.ToIniValues());
            this.ConfigAddNPCSpawnEntriesContainer.IsEnabled = this.ConfigAddNPCSpawnEntriesContainer.Count > 0;

            this.ConfigSubtractNPCSpawnEntriesContainer.Clear();
            this.ConfigSubtractNPCSpawnEntriesContainer.FromIniValues(sourceProfile.ConfigSubtractNPCSpawnEntriesContainer.ToIniValues());
            this.ConfigSubtractNPCSpawnEntriesContainer.IsEnabled = this.ConfigSubtractNPCSpawnEntriesContainer.Count > 0;

            this.ConfigOverrideNPCSpawnEntriesContainer.Clear();
            this.ConfigOverrideNPCSpawnEntriesContainer.FromIniValues(sourceProfile.ConfigOverrideNPCSpawnEntriesContainer.ToIniValues());
            this.ConfigOverrideNPCSpawnEntriesContainer.IsEnabled = this.ConfigOverrideNPCSpawnEntriesContainer.Count > 0;

            this.NPCSpawnSettings = new NPCSpawnSettingsList(this.ConfigAddNPCSpawnEntriesContainer, this.ConfigSubtractNPCSpawnEntriesContainer, this.ConfigOverrideNPCSpawnEntriesContainer);
            this.NPCSpawnSettings.RenderToView();
        }

        private void SyncPGMSection(ServerProfile sourceProfile)
        {
            this.SetValue(PGM_EnabledProperty, sourceProfile.PGM_Enabled);
            this.SetValue(PGM_NameProperty, sourceProfile.PGM_Name);

            this.PGM_Terrain.InitializeFromINIValue(sourceProfile.PGM_Terrain.ToINIValue());
        }

        private void SyncPlayerSettingsSection(ServerProfile sourceProfile)
        {
            this.SetValue(EnableFlyerCarryProperty, sourceProfile.EnableFlyerCarry);
            this.SetValue(XPMultiplierProperty, sourceProfile.XPMultiplier);
            this.SetValue(PlayerDamageMultiplierProperty, sourceProfile.PlayerDamageMultiplier);
            this.SetValue(PlayerResistanceMultiplierProperty, sourceProfile.PlayerResistanceMultiplier);
            this.SetValue(PlayerCharacterWaterDrainMultiplierProperty, sourceProfile.PlayerCharacterWaterDrainMultiplier);
            this.SetValue(PlayerCharacterFoodDrainMultiplierProperty, sourceProfile.PlayerCharacterFoodDrainMultiplier);
            this.SetValue(PlayerCharacterStaminaDrainMultiplierProperty, sourceProfile.PlayerCharacterStaminaDrainMultiplier);
            this.SetValue(PlayerCharacterHealthRecoveryMultiplierProperty, sourceProfile.PlayerCharacterHealthRecoveryMultiplier);
            this.SetValue(PlayerHarvestingDamageMultiplierProperty, sourceProfile.PlayerHarvestingDamageMultiplier);
            this.SetValue(CraftingSkillBonusMultiplierProperty, sourceProfile.CraftingSkillBonusMultiplier);
            this.SetValue(MaxFallSpeedMultiplierProperty, sourceProfile.MaxFallSpeedMultiplier);

            this.PlayerBaseStatMultipliers.Clear();
            this.PlayerBaseStatMultipliers.FromIniValues(sourceProfile.PlayerBaseStatMultipliers.ToIniValues());
            this.PlayerBaseStatMultipliers.IsEnabled = sourceProfile.PlayerBaseStatMultipliers.IsEnabled;

            this.PerLevelStatsMultiplier_Player.Clear();
            this.PerLevelStatsMultiplier_Player.FromIniValues(sourceProfile.PerLevelStatsMultiplier_Player.ToIniValues());
            this.PerLevelStatsMultiplier_Player.IsEnabled = sourceProfile.PerLevelStatsMultiplier_Player.IsEnabled;
        }

        private void SyncPreventTransferOverridesSection(ServerProfile sourceProfile)
        {
            sourceProfile.PreventTransferForClassNames.RenderToModel();

            this.PreventTransferForClassNames.Clear();
            this.PreventTransferForClassNames.FromIniValues(sourceProfile.PreventTransferForClassNames.ToIniValues());
            this.PreventTransferForClassNames.IsEnabled = this.PreventTransferForClassNames.Count > 0;
            this.PreventTransferForClassNames.RenderToView();
        }

        private void SyncRulesSection(ServerProfile sourceProfile)
        {
            this.SetValue(EnableHardcoreProperty, sourceProfile.EnableHardcore);
            this.SetValue(EnablePVPProperty, sourceProfile.EnablePVP);
            this.SetValue(EnableCreativeModeProperty, sourceProfile.EnableCreativeMode);
            this.SetValue(AllowCaveBuildingPvEProperty, sourceProfile.AllowCaveBuildingPvE);
            this.SetValue(DisableFriendlyFirePvPProperty, sourceProfile.DisableFriendlyFirePvP);
            this.SetValue(DisableFriendlyFirePvEProperty, sourceProfile.DisableFriendlyFirePvE);
            this.SetValue(AllowCaveBuildingPvPProperty, sourceProfile.AllowCaveBuildingPvP);
            this.SetValue(DisableRailgunPVPProperty, sourceProfile.DisableRailgunPVP);
            this.SetValue(DisableLootCratesProperty, sourceProfile.DisableLootCrates);
            this.SetValue(AllowCrateSpawnsOnTopOfStructuresProperty, sourceProfile.AllowCrateSpawnsOnTopOfStructures);
            this.SetValue(EnableExtraStructurePreventionVolumesProperty, sourceProfile.EnableExtraStructurePreventionVolumes);
            this.SetValue(UseSingleplayerSettingsProperty, sourceProfile.UseSingleplayerSettings);

            this.SetValue(EnableDifficultyOverrideProperty, sourceProfile.EnableDifficultyOverride);
            this.SetValue(OverrideOfficialDifficultyProperty, sourceProfile.OverrideOfficialDifficulty);
            this.SetValue(DifficultyOffsetProperty, sourceProfile.DifficultyOffset);
            this.SetValue(DestroyTamesOverLevelClamp﻿Property, sourceProfile.DestroyTamesOverLevelClamp﻿);

            this.SetValue(EnableTributeDownloadsProperty, sourceProfile.EnableTributeDownloads);
            this.SetValue(PreventDownloadSurvivorsProperty, sourceProfile.PreventDownloadSurvivors);
            this.SetValue(PreventDownloadItemsProperty, sourceProfile.PreventDownloadItems);
            this.SetValue(PreventDownloadDinosProperty, sourceProfile.PreventDownloadDinos);
            this.SetValue(PreventUploadSurvivorsProperty, sourceProfile.PreventUploadSurvivors);
            this.SetValue(PreventUploadItemsProperty, sourceProfile.PreventUploadItems);
            this.SetValue(PreventUploadDinosProperty, sourceProfile.PreventUploadDinos);
            this.SetNullableValue(MaxTributeDinosProperty, sourceProfile.MaxTributeDinos);
            this.SetNullableValue(MaxTributeItemsProperty, sourceProfile.MaxTributeItems);

            this.SetValue(NoTransferFromFilteringProperty, sourceProfile.NoTransferFromFiltering);
            this.SetValue(DisableCustomFoldersInTributeInventoriesProperty, sourceProfile.DisableCustomFoldersInTributeInventories);
            this.SetValue(OverrideTributeCharacterExpirationSecondsProperty, sourceProfile.OverrideTributeCharacterExpirationSeconds);
            this.SetValue(OverrideTributeItemExpirationSecondsProperty, sourceProfile.OverrideTributeItemExpirationSeconds);
            this.SetValue(OverrideTributeDinoExpirationSecondsProperty, sourceProfile.OverrideTributeDinoExpirationSeconds);
            this.SetValue(OverrideMinimumDinoReuploadIntervalProperty, sourceProfile.OverrideMinimumDinoReuploadInterval);
            this.SetValue(TributeCharacterExpirationSecondsProperty, sourceProfile.TributeCharacterExpirationSeconds);
            this.SetValue(TributeItemExpirationSecondsProperty, sourceProfile.TributeItemExpirationSeconds);
            this.SetValue(TributeDinoExpirationSecondsProperty, sourceProfile.TributeDinoExpirationSeconds);
            this.SetValue(MinimumDinoReuploadIntervalProperty, sourceProfile.MinimumDinoReuploadInterval);
            this.SetValue(CrossARKAllowForeignDinoDownloadsProperty, sourceProfile.CrossARKAllowForeignDinoDownloads);

            this.SetValue(IncreasePvPRespawnIntervalProperty, sourceProfile.IncreasePvPRespawnInterval);
            this.SetValue(IncreasePvPRespawnIntervalCheckPeriodProperty, sourceProfile.IncreasePvPRespawnIntervalCheckPeriod);
            this.SetValue(IncreasePvPRespawnIntervalMultiplierProperty, sourceProfile.IncreasePvPRespawnIntervalMultiplier);
            this.SetValue(IncreasePvPRespawnIntervalBaseAmountProperty, sourceProfile.IncreasePvPRespawnIntervalBaseAmount);

            this.SetValue(PreventOfflinePvPProperty, sourceProfile.PreventOfflinePvP);
            this.SetValue(PreventOfflinePvPIntervalProperty, sourceProfile.PreventOfflinePvPInterval);
            this.SetValue(PreventOfflinePvPConnectionInvincibleIntervalProperty, sourceProfile.PreventOfflinePvPConnectionInvincibleInterval);

            this.SetValue(AutoPvETimerProperty, sourceProfile.AutoPvETimer);
            this.SetValue(AutoPvEUseSystemTimeProperty, sourceProfile.AutoPvEUseSystemTime);
            this.SetValue(AutoPvEStartTimeSecondsProperty, sourceProfile.AutoPvEStartTimeSeconds);
            this.SetValue(AutoPvEStopTimeSecondsProperty, sourceProfile.AutoPvEStopTimeSeconds);

            this.SetValue(MaxNumberOfPlayersInTribeProperty, sourceProfile.MaxNumberOfPlayersInTribe);
            this.SetValue(TribeNameChangeCooldownProperty, sourceProfile.TribeNameChangeCooldown);
            this.SetValue(TribeSlotReuseCooldownProperty, sourceProfile.TribeSlotReuseCooldown);
            this.SetValue(AllowTribeAlliancesProperty, sourceProfile.AllowTribeAlliances);
            this.SetValue(MaxAlliancesPerTribeProperty, sourceProfile.MaxAlliancesPerTribe);
            this.SetValue(MaxTribesPerAllianceProperty, sourceProfile.MaxTribesPerAlliance);
            this.SetValue(AllowTribeWarPvEProperty, sourceProfile.AllowTribeWarPvE);
            this.SetValue(AllowTribeWarCancelPvEProperty, sourceProfile.AllowTribeWarCancelPvE);

            this.SetValue(AllowCustomRecipesProperty, sourceProfile.AllowCustomRecipes);
            this.SetValue(CustomRecipeEffectivenessMultiplierProperty, sourceProfile.CustomRecipeEffectivenessMultiplier);
            this.SetValue(CustomRecipeSkillMultiplierProperty, sourceProfile.CustomRecipeSkillMultiplier);

            this.SetValue(EnableDiseasesProperty, sourceProfile.EnableDiseases);
            this.SetValue(NonPermanentDiseasesProperty, sourceProfile.NonPermanentDiseases);

            this.SetValue(OverrideNPCNetworkStasisRangeScaleProperty, sourceProfile.OverrideNPCNetworkStasisRangeScale);
            this.SetValue(NPCNetworkStasisRangeScalePlayerCountStartProperty, sourceProfile.NPCNetworkStasisRangeScalePlayerCountStart);
            this.SetValue(NPCNetworkStasisRangeScalePlayerCountEndProperty, sourceProfile.NPCNetworkStasisRangeScalePlayerCountEnd);
            this.SetValue(NPCNetworkStasisRangeScalePercentEndProperty, sourceProfile.NPCNetworkStasisRangeScalePercentEnd);

            this.SetValue(UseCorpseLocatorProperty, sourceProfile.UseCorpseLocator);
            this.SetValue(PreventSpawnAnimationsProperty, sourceProfile.PreventSpawnAnimations);
            this.SetValue(AllowUnlimitedRespecsProperty, sourceProfile.AllowUnlimitedRespecs);
            this.SetValue(AllowPlatformSaddleMultiFloorsProperty, sourceProfile.AllowPlatformSaddleMultiFloors);
            this.SetValue(PlatformSaddleBuildAreaBoundsMultiplierProperty, sourceProfile.PlatformSaddleBuildAreaBoundsMultiplier);
            this.SetValue(MaxGateFrameOnSaddlesProperty, sourceProfile.MaxGateFrameOnSaddles);
            this.SetValue(OxygenSwimSpeedStatMultiplierProperty, sourceProfile.OxygenSwimSpeedStatMultiplier);
            this.SetValue(SupplyCrateLootQualityMultiplierProperty, sourceProfile.SupplyCrateLootQualityMultiplier);
            this.SetValue(FishingLootQualityMultiplierProperty, sourceProfile.FishingLootQualityMultiplier);
            this.SetValue(UseCorpseLifeSpanMultiplierProperty, sourceProfile.UseCorpseLifeSpanMultiplier);
            this.SetValue(MinimumTimeBetweenInventoryRetrievalProperty, sourceProfile.MinimumTimeBetweenInventoryRetrieval);
            this.SetValue(GlobalPoweredBatteryDurabilityDecreasePerSecondProperty, sourceProfile.GlobalPoweredBatteryDurabilityDecreasePerSecond);
            this.SetValue(RandomSupplyCratePointsProperty, sourceProfile.RandomSupplyCratePoints);
            this.SetValue(FuelConsumptionIntervalMultiplierProperty, sourceProfile.FuelConsumptionIntervalMultiplier);
            this.SetValue(LimitNonPlayerDroppedItemsRangeProperty, sourceProfile.LimitNonPlayerDroppedItemsRange);
            this.SetValue(LimitNonPlayerDroppedItemsCountProperty, sourceProfile.LimitNonPlayerDroppedItemsCount);

            this.SetValue(EnableCryoSicknessPVEProperty, sourceProfile.EnableCryoSicknessPVE);
            this.SetValue(EnableCryopodNerfProperty, sourceProfile.EnableCryopodNerf);
            this.SetValue(CryopodNerfDurationProperty, sourceProfile.CryopodNerfDuration);
            this.SetValue(CryopodNerfDamageMultiplierProperty, sourceProfile.CryopodNerfDamageMultiplier);
            this.SetValue(CryopodNerfIncomingDamageMultiplierPercentProperty, sourceProfile.CryopodNerfIncomingDamageMultiplierPercent);

            this.SetValue(AllowTekSuitPowersInGenesisProperty, sourceProfile.AllowTekSuitPowersInGenesis);
            this.SetValue(DisableGenesisMissionsProperty, sourceProfile.DisableGenesisMissions);
            this.SetValue(DisableDefaultMapItemSetsProperty, sourceProfile.DisableDefaultMapItemSets);
            this.SetValue(DisableWorldBuffsProperty, sourceProfile.DisableWorldBuffs);
            this.SetValue(EnableWorldBuffScalingProperty, sourceProfile.EnableWorldBuffScaling);
            this.SetValue(WorldBuffScalingEfficacyProperty, sourceProfile.WorldBuffScalingEfficacy);
            this.SetValue(AdjustableMutagenSpawnDelayMultiplierProperty, sourceProfile.AdjustableMutagenSpawnDelayMultiplier);

            this.SetValue(MaxHexagonsPerCharacterProperty, sourceProfile.MaxHexagonsPerCharacter);
            this.SetValue(DisableHexagonStoreProperty, sourceProfile.DisableHexagonStore);
            this.SetValue(HexStoreAllowOnlyEngramTradeOptionProperty, sourceProfile.HexStoreAllowOnlyEngramTradeOption);
            this.SetValue(HexagonRewardMultiplierProperty, sourceProfile.HexagonRewardMultiplier);
            this.SetValue(HexagonCostMultiplierProperty, sourceProfile.HexagonCostMultiplier);

            this.SetValue(Ragnarok_EnableSettingsProperty, sourceProfile.Ragnarok_EnableSettings);
            this.SetValue(Ragnarok_AllowMultipleTamedUnicornsProperty, sourceProfile.Ragnarok_AllowMultipleTamedUnicorns);
            this.SetValue(Ragnarok_UnicornSpawnIntervalProperty, sourceProfile.Ragnarok_UnicornSpawnInterval);
            this.SetValue(Ragnarok_EnableVolcanoProperty, sourceProfile.Ragnarok_EnableVolcano);
            this.SetValue(Ragnarok_VolcanoIntervalProperty, sourceProfile.Ragnarok_VolcanoInterval);
            this.SetValue(Ragnarok_VolcanoIntensityProperty, sourceProfile.Ragnarok_VolcanoIntensity);

            this.SetValue(Fjordur_EnableSettingsProperty, sourceProfile.Fjordur_EnableSettings);
            this.SetValue(UseFjordurTraversalBuffProperty, sourceProfile.UseFjordurTraversalBuff);

            this.SetNullableValue(ItemStatClamps_GenericQualityProperty, sourceProfile.ItemStatClamps_GenericQuality);
            this.SetNullableValue(ItemStatClamps_ArmorProperty, sourceProfile.ItemStatClamps_Armor);
            this.SetNullableValue(ItemStatClamps_MaxDurabilityProperty, sourceProfile.ItemStatClamps_MaxDurability);
            this.SetNullableValue(ItemStatClamps_WeaponDamagePercentProperty, sourceProfile.ItemStatClamps_WeaponDamagePercent);
            this.SetNullableValue(ItemStatClamps_WeaponClipAmmoProperty, sourceProfile.ItemStatClamps_WeaponClipAmmo);
            this.SetNullableValue(ItemStatClamps_HypothermalInsulationProperty, sourceProfile.ItemStatClamps_HypothermalInsulation);
            this.SetNullableValue(ItemStatClamps_WeightProperty, sourceProfile.ItemStatClamps_Weight);
            this.SetNullableValue(ItemStatClamps_HyperthermalInsulationProperty, sourceProfile.ItemStatClamps_HyperthermalInsulation);
        }

        private void SyncServerDetails(ServerProfile sourceProfile)
        {
            this.SetValue(BranchNameProperty, sourceProfile.BranchName);
            this.SetValue(BranchPasswordProperty, sourceProfile.BranchPassword);

            this.SetValue(EventNameProperty, sourceProfile.EventName);
            this.SetValue(EventColorsChanceOverrideProperty, sourceProfile.EventColorsChanceOverride);
            this.SetNullableValue(NewYear1UTCProperty, sourceProfile.NewYear1UTC);
            this.SetNullableValue(NewYear2UTCProperty, sourceProfile.NewYear2UTC);
        }

        private void SyncServerFiles(ServerProfile sourceProfile)
        {
            this.SetValue(ServerFilesAdminsProperty, sourceProfile.ServerFilesAdmins);
            this.SetValue(EnableExclusiveJoinProperty, sourceProfile.EnableExclusiveJoin);
            this.SetValue(ServerFilesExclusiveProperty, sourceProfile.ServerFilesExclusive);
            this.SetValue(ServerFilesWhitelistedProperty, sourceProfile.ServerFilesWhitelisted);

            SaveServerFileAdministrators();
            SaveServerFileExclusive();
            SaveServerFileWhitelisted();
        }

        private void SyncSOTFSection(ServerProfile sourceProfile)
        {
            this.SetValue(SOTF_EnabledProperty, sourceProfile.SOTF_Enabled);
            this.SetValue(SOTF_DisableDeathSPectatorProperty, sourceProfile.SOTF_DisableDeathSPectator);
            this.SetValue(SOTF_OnlyAdminRejoinAsSpectatorProperty, sourceProfile.SOTF_OnlyAdminRejoinAsSpectator);
            this.SetValue(SOTF_GamePlayLoggingProperty, sourceProfile.SOTF_GamePlayLogging);
            this.SetValue(SOTF_OutputGameReportProperty, sourceProfile.SOTF_OutputGameReport);
            this.SetValue(SOTF_MaxNumberOfPlayersInTribeProperty, sourceProfile.SOTF_MaxNumberOfPlayersInTribe);
            this.SetValue(SOTF_BattleNumOfTribesToStartGameProperty, sourceProfile.SOTF_BattleNumOfTribesToStartGame);
            this.SetValue(SOTF_TimeToCollapseRODProperty, sourceProfile.SOTF_TimeToCollapseROD);
            this.SetValue(SOTF_BattleAutoStartGameIntervalProperty, sourceProfile.SOTF_BattleAutoStartGameInterval);
            this.SetValue(SOTF_BattleAutoRestartGameIntervalProperty, sourceProfile.SOTF_BattleAutoRestartGameInterval);
            this.SetValue(SOTF_BattleSuddenDeathIntervalProperty, sourceProfile.SOTF_BattleSuddenDeathInterval);

            this.SetValue(SOTF_NoEventsProperty, sourceProfile.SOTF_NoEvents);
            this.SetValue(SOTF_NoBossesProperty, sourceProfile.SOTF_NoBosses);
            this.SetValue(SOTF_BothBossesProperty, sourceProfile.SOTF_BothBosses);
            this.SetValue(SOTF_EvoEventIntervalProperty, sourceProfile.SOTF_EvoEventInterval);
            this.SetValue(SOTF_RingStartTimeProperty, sourceProfile.SOTF_RingStartTime);
        }

        private void SyncStackSizeOverridesSection(ServerProfile sourceProfile)
        {
            this.SetValue(ItemStackSizeMultiplierProperty, sourceProfile.ItemStackSizeMultiplier);

            sourceProfile.ConfigOverrideItemMaxQuantity.RenderToModel();

            this.ConfigOverrideItemMaxQuantity.Clear();
            this.ConfigOverrideItemMaxQuantity.FromIniValues(sourceProfile.ConfigOverrideItemMaxQuantity.ToIniValues());
            this.ConfigOverrideItemMaxQuantity.IsEnabled = this.ConfigOverrideItemMaxQuantity.Count > 0;
            this.ConfigOverrideItemMaxQuantity.RenderToView();
        }

        private void SyncStructuresSection(ServerProfile sourceProfile)
        {
            this.SetValue(StructureResistanceMultiplierProperty, sourceProfile.StructureResistanceMultiplier);
            this.SetValue(StructureDamageMultiplierProperty, sourceProfile.StructureDamageMultiplier);
            this.SetValue(StructureDamageRepairCooldownProperty, sourceProfile.StructureDamageRepairCooldown);
            this.SetValue(PvPStructureDecayProperty, sourceProfile.PvPStructureDecay);
            this.SetValue(PvPZoneStructureDamageMultiplierProperty, sourceProfile.PvPZoneStructureDamageMultiplier);
            this.SetValue(MaxStructuresInRangeProperty, sourceProfile.MaxStructuresInRange);
            this.SetValue(PerPlatformMaxStructuresMultiplierProperty, sourceProfile.PerPlatformMaxStructuresMultiplier);
            this.SetValue(MaxPlatformSaddleStructureLimitProperty, sourceProfile.MaxPlatformSaddleStructureLimit);
            this.SetValue(OverrideStructurePlatformPreventionProperty, sourceProfile.OverrideStructurePlatformPrevention);
            this.SetValue(FlyerPlatformAllowUnalignedDinoBasingProperty, sourceProfile.FlyerPlatformAllowUnalignedDinoBasing);
            this.SetValue(PvEAllowStructuresAtSupplyDropsProperty, sourceProfile.PvEAllowStructuresAtSupplyDrops);
            this.SetValue(EnableStructureDecayPvEProperty, sourceProfile.EnableStructureDecayPvE);
            this.SetValue(PvEStructureDecayPeriodMultiplierProperty, sourceProfile.PvEStructureDecayPeriodMultiplier);
            this.SetValue(AutoDestroyOldStructuresMultiplierProperty, sourceProfile.AutoDestroyOldStructuresMultiplier);
            this.SetValue(ForceAllStructureLockingProperty, sourceProfile.ForceAllStructureLocking);
            this.SetValue(PassiveDefensesDamageRiderlessDinosProperty, sourceProfile.PassiveDefensesDamageRiderlessDinos);
            this.SetValue(EnableAutoDestroyStructuresProperty, sourceProfile.EnableAutoDestroyStructures);
            this.SetValue(OnlyAutoDestroyCoreStructuresProperty, sourceProfile.OnlyAutoDestroyCoreStructures);
            this.SetValue(OnlyDecayUnsnappedCoreStructuresProperty, sourceProfile.OnlyDecayUnsnappedCoreStructures);
            this.SetValue(FastDecayUnsnappedCoreStructuresProperty, sourceProfile.FastDecayUnsnappedCoreStructures);
            this.SetValue(DestroyUnconnectedWaterPipesProperty, sourceProfile.DestroyUnconnectedWaterPipes);
            this.SetValue(DisableStructurePlacementCollisionProperty, sourceProfile.DisableStructurePlacementCollision);
            this.SetValue(IgnoreLimitMaxStructuresInRangeTypeFlagProperty, sourceProfile.IgnoreLimitMaxStructuresInRangeTypeFlag);
            this.SetValue(EnableFastDecayIntervalProperty, sourceProfile.EnableFastDecayInterval);
            this.SetValue(FastDecayIntervalProperty, sourceProfile.FastDecayInterval);
            this.SetValue(LimitTurretsInRangeProperty, sourceProfile.LimitTurretsInRange);
            this.SetValue(LimitTurretsRangeProperty, sourceProfile.LimitTurretsRange);
            this.SetValue(LimitTurretsNumProperty, sourceProfile.LimitTurretsNum);
            this.SetValue(HardLimitTurretsInRangeProperty, sourceProfile.HardLimitTurretsInRange);
            this.SetValue(AlwaysAllowStructurePickupProperty, sourceProfile.AlwaysAllowStructurePickup);
            this.SetValue(StructurePickupTimeAfterPlacementProperty, sourceProfile.StructurePickupTimeAfterPlacement);
            this.SetValue(StructurePickupHoldDurationProperty, sourceProfile.StructurePickupHoldDuration);
            this.SetValue(AllowIntegratedSPlusStructuresProperty, sourceProfile.AllowIntegratedSPlusStructures);
            this.SetValue(IgnoreStructuresPreventionVolumesProperty, sourceProfile.IgnoreStructuresPreventionVolumes);
            this.SetValue(GenesisUseStructuresPreventionVolumesProperty, sourceProfile.GenesisUseStructuresPreventionVolumes);
        }

        private void SyncSupplyCrateOverridesSection(ServerProfile sourceProfile)
        {
            sourceProfile.ConfigOverrideSupplyCrateItems.RenderToModel();

            this.ConfigOverrideSupplyCrateItems.Clear();
            this.ConfigOverrideSupplyCrateItems.FromIniValues(sourceProfile.ConfigOverrideSupplyCrateItems.ToIniValues());
            this.ConfigOverrideSupplyCrateItems.IsEnabled = this.ConfigOverrideSupplyCrateItems.Count > 0;
            this.ConfigOverrideSupplyCrateItems.RenderToView();
        }

        private void SyncExcludeItemIndicesOverridesSection(ServerProfile sourceProfile)
        {
            sourceProfile.ExcludeItemIndices.RenderToModel();

            this.ExcludeItemIndices.Clear();
            this.ExcludeItemIndices.FromIniValues(sourceProfile.ExcludeItemIndices.ToIniValues());
            this.ExcludeItemIndices.IsEnabled = this.ExcludeItemIndices.Count > 0;
            this.ExcludeItemIndices.RenderToView();
        }
        #endregion

        #region Server Files
        private void ServerFilesWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var adminFile = false;
            var exclusiveFile = false;
            var whitelistFile = false;

            if (e.Name.Equals(Config.Default.ServerAdminFile, StringComparison.OrdinalIgnoreCase))
            {
                adminFile = true;
            }
            if (e.Name.Equals(Config.Default.ServerExclusiveFile, StringComparison.OrdinalIgnoreCase))
            {
                exclusiveFile = true;
            }
            if (e.Name.Equals(Config.Default.ServerWhitelistFile, StringComparison.OrdinalIgnoreCase))
            {
                whitelistFile = true;
            }

            TaskUtils.RunOnUIThreadAsync(() => LoadServerFiles(adminFile, exclusiveFile, whitelistFile)).DoNotWait();
        }

        private void ServerFilesWatcher_Error(object sender, ErrorEventArgs e)
        {
            TaskUtils.RunOnUIThreadAsync(() => SetupServerFilesWatcher()).DoNotWait();
        }

        public void DestroyServerFilesWatcher()
        {
            if (_serverFilesWatcherBinary != null)
            {
                _serverFilesWatcherBinary.EnableRaisingEvents = false;
                _serverFilesWatcherBinary.Changed -= ServerFilesWatcher_Changed;
                _serverFilesWatcherBinary.Created -= ServerFilesWatcher_Changed;
                _serverFilesWatcherBinary.Deleted -= ServerFilesWatcher_Changed;
                _serverFilesWatcherBinary.Error -= ServerFilesWatcher_Error;
                _serverFilesWatcherBinary = null;
            }

            if (_serverFilesWatcherSaved != null)
            {
                _serverFilesWatcherSaved.EnableRaisingEvents = false;
                _serverFilesWatcherSaved.Changed -= ServerFilesWatcher_Changed;
                _serverFilesWatcherSaved.Created -= ServerFilesWatcher_Changed;
                _serverFilesWatcherSaved.Deleted -= ServerFilesWatcher_Changed;
                _serverFilesWatcherSaved.Error -= ServerFilesWatcher_Error;
                _serverFilesWatcherSaved = null;
            }
        }

        public void SetupServerFilesWatcher()
        {
            if (_serverFilesWatcherBinary != null || _serverFilesWatcherSaved != null)
                DestroyServerFilesWatcher();

            if (!EnableServerFilesWatcher)
                return;

            var path = Path.Combine(InstallDirectory, Config.Default.ServerBinaryRelativePath);
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return;

            _serverFilesWatcherBinary = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite,
            };

            _serverFilesWatcherBinary.Changed += ServerFilesWatcher_Changed;
            _serverFilesWatcherBinary.Created += ServerFilesWatcher_Changed;
            _serverFilesWatcherBinary.Deleted += ServerFilesWatcher_Changed;
            _serverFilesWatcherBinary.Error += ServerFilesWatcher_Error;
            _serverFilesWatcherBinary.EnableRaisingEvents = true;

            path = Path.Combine(InstallDirectory, Config.Default.SavedRelativePath);
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return;

            _serverFilesWatcherSaved = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite,
            };

            _serverFilesWatcherSaved.Changed += ServerFilesWatcher_Changed;
            _serverFilesWatcherSaved.Created += ServerFilesWatcher_Changed;
            _serverFilesWatcherSaved.Deleted += ServerFilesWatcher_Changed;
            _serverFilesWatcherSaved.Error += ServerFilesWatcher_Error;
            _serverFilesWatcherSaved.EnableRaisingEvents = true;
        }

        public void LoadServerFiles(bool adminFile, bool exclusiveFile, bool whitelistFile)
        {
            try
            {
                var list1 = this.ServerFilesAdmins ?? new PlayerUserList();
                var list2 = this.ServerFilesExclusive ?? new PlayerUserList();
                var list3 = this.ServerFilesWhitelisted ?? new PlayerUserList();

                var allSteamIds = new List<string>();
                string[] adminSteamIds = null;
                string[] exclusiveSteamIds = null;
                string[] whitelistSteamIds = null;

                if (adminFile)
                {
                    var file = Path.Combine(InstallDirectory, Config.Default.SavedRelativePath, Config.Default.ServerAdminFile);
                    if (File.Exists(file))
                    {
                        adminSteamIds = File.ReadAllLines(file);
                        allSteamIds.AddRange(adminSteamIds);
                    }
                }

                if (exclusiveFile)
                {
                    var file = Path.Combine(InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExclusiveFile);
                    if (File.Exists(file))
                    {
                        exclusiveSteamIds = File.ReadAllLines(file);
                        allSteamIds.AddRange(exclusiveSteamIds);
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

                if (adminFile && adminSteamIds != null)
                {
                    list1 = PlayerUserList.GetList(steamUsers, adminSteamIds);
                }

                if (exclusiveFile && exclusiveSteamIds != null)
                {
                    list2 = PlayerUserList.GetList(steamUsers, exclusiveSteamIds);
                }

                if (whitelistFile && whitelistSteamIds != null)
                {
                    list3 = PlayerUserList.GetList(steamUsers, whitelistSteamIds);
                }

                this.ServerFilesAdmins = list1;
                this.ServerFilesExclusive = list2;
                this.ServerFilesWhitelisted = list3;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ServerFilesLoadErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveServerFileAdministrators()
        {
            try
            {
                var folder = Path.Combine(InstallDirectory, Config.Default.SavedRelativePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var file = Path.Combine(folder, Config.Default.ServerAdminFile);
                File.WriteAllLines(file, this.ServerFilesAdmins.ToEnumerable());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ServerFilesSaveErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveServerFileExclusive()
        {
            try
            {
                var folder = Path.Combine(InstallDirectory, Config.Default.ServerBinaryRelativePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var file = Path.Combine(folder, Config.Default.ServerExclusiveFile);
                File.WriteAllLines(file, this.ServerFilesExclusive.ToEnumerable());
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
                var folder = Path.Combine(InstallDirectory, Config.Default.ServerBinaryRelativePath);
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
        public static string GetProfileMapFileName(ServerProfile profile)
        {
            return GetProfileMapFileName(profile?.ServerMap, profile?.PGM_Enabled ?? false, profile?.PGM_Name);
        }

        public static string GetProfileMapFileName(string serverMap, bool pgmEnabled, string pgmName)
        {
            if (pgmEnabled)
                return $"{pgmName ?? string.Empty}_V2";

            return ModUtils.GetMapName(serverMap);
        }

        public static string GetProfileMapModId(ServerProfile profile)
        {
            return GetProfileMapModId(profile?.ServerMap, profile?.PGM_Enabled ?? false);
        }

        public static string GetProfileMapModId(string serverMap, bool pgmEnabled)
        {
            if (pgmEnabled)
                return string.Empty;

            return ModUtils.GetMapModId(serverMap);
        }

        public static string GetProfileMapName(ServerProfile profile)
        {
            return GetProfileMapName(profile?.ServerMap, profile?.PGM_Enabled ?? false);
        }

        public static string GetProfileMapName(string serverMap, bool pgmEnabled)
        {
            if (pgmEnabled)
                return Config.Default.DefaultServerMap_PGM;

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
            return GetProfileSavePath(profile?.InstallDirectory, profile?.AltSaveDirectoryName, profile?.PGM_Enabled ?? false, profile?.PGM_Name);
        }

        public static string GetProfileSavePath(string installDirectory, string altSaveDirectoryName, bool pgmEnabled, string pgmName)
        {
            if (!string.IsNullOrWhiteSpace(altSaveDirectoryName))
            {
                if (pgmEnabled)
                    return Path.Combine(installDirectory ?? string.Empty, Config.Default.SavedRelativePath, altSaveDirectoryName, Config.Default.SavedPGMRelativePath, pgmName ?? string.Empty);
                return Path.Combine(installDirectory ?? string.Empty, Config.Default.SavedRelativePath, altSaveDirectoryName);
            }

            if (pgmEnabled)
                return Path.Combine(installDirectory ?? string.Empty, Config.Default.SavedFilesRelativePath, Config.Default.SavedPGMRelativePath, pgmName ?? string.Empty);
            return Path.Combine(installDirectory ?? string.Empty, Config.Default.SavedFilesRelativePath);
        }
        #endregion

        #endregion
    }
}
