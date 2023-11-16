using Microsoft.WindowsAPICodePack.Dialogs;
using ServerManagerTool.Common;
using ServerManagerTool.Common.Controls;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Serialization;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using ServerManagerTool.Lib.Model;
using ServerManagerTool.Lib.ViewModel;
using ServerManagerTool.Plugin.Common;
using ServerManagerTool.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    partial class ServerSettingsControl : UserControl
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private CancellationTokenSource _upgradeCancellationSource = null;
        private FindSettingWindow _findSettingWindow = null;
        private FindSettingItem _lastFoundSetting = null;

        // Using a DependencyProperty as the backing store for ServerManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BaseDinoModListProperty = DependencyProperty.Register(nameof(BaseDinoModList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseEngramModListProperty = DependencyProperty.Register(nameof(BaseEngramModList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseResourceModListProperty = DependencyProperty.Register(nameof(BaseResourceModList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseDinoListProperty = DependencyProperty.Register(nameof(BaseDinoList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseMapSpawnerListProperty = DependencyProperty.Register(nameof(BaseMapSpawnerList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BasePrimalItemListProperty = DependencyProperty.Register(nameof(BasePrimalItemList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseSupplyCrateListProperty = DependencyProperty.Register(nameof(BaseSupplyCrateList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseGameMapsProperty = DependencyProperty.Register(nameof(BaseGameMaps), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseTotalConversionsProperty = DependencyProperty.Register(nameof(BaseTotalConversions), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseBranchesProperty = DependencyProperty.Register(nameof(BaseBranches), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseEventsProperty = DependencyProperty.Register(nameof(BaseEvents), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BetaVersionProperty = DependencyProperty.Register(nameof(BetaVersion), typeof(bool), typeof(ServerSettingsControl), new PropertyMetadata(false));
        public static readonly DependencyProperty ConfigProperty = DependencyProperty.Register(nameof(Config), typeof(Config), typeof(ServerSettingsControl));
        public static readonly DependencyProperty CultureProperty = DependencyProperty.Register(nameof(Culture), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty IsAdministratorProperty = DependencyProperty.Register(nameof(IsAdministrator), typeof(bool), typeof(ServerSettingsControl), new PropertyMetadata(false));
        public static readonly DependencyProperty NetworkInterfacesProperty = DependencyProperty.Register(nameof(NetworkInterfaces), typeof(List<NetworkAdapterEntry>), typeof(ServerSettingsControl), new PropertyMetadata(new List<NetworkAdapterEntry>()));
        public static readonly DependencyProperty RuntimeProperty = DependencyProperty.Register(nameof(Runtime), typeof(ServerRuntime), typeof(ServerSettingsControl));
        public static readonly DependencyProperty ServerManagerProperty = DependencyProperty.Register(nameof(ServerManager), typeof(ServerManager), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ServerProperty = DependencyProperty.Register(nameof(Server), typeof(Server), typeof(ServerSettingsControl), new PropertyMetadata(null, ServerPropertyChanged));
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(ServerProfile), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedModDinoProperty = DependencyProperty.Register(nameof(SelectedModDino), typeof(string), typeof(ServerSettingsControl), new PropertyMetadata("All"));
        public static readonly DependencyProperty SelectedModEngramProperty = DependencyProperty.Register(nameof(SelectedModEngram), typeof(string), typeof(ServerSettingsControl), new PropertyMetadata("All"));
        public static readonly DependencyProperty SelectedModResourceProperty = DependencyProperty.Register(nameof(SelectedModResource), typeof(string), typeof(ServerSettingsControl), new PropertyMetadata("All"));
        public static readonly DependencyProperty SelectedCraftingOverrideProperty = DependencyProperty.Register(nameof(SelectedCraftingOverride), typeof(CraftingOverride), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedCustomEngineSettingProperty = DependencyProperty.Register(nameof(SelectedCustomEngineSetting), typeof(CustomSection), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedCustomGameSettingProperty = DependencyProperty.Register(nameof(SelectedCustomGameSetting), typeof(CustomSection), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedCustomGameUserSettingProperty = DependencyProperty.Register(nameof(SelectedCustomGameUserSetting), typeof(CustomSection), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedNPCSpawnSettingProperty = DependencyProperty.Register(nameof(SelectedNPCSpawnSetting), typeof(NPCSpawnSettings), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedSupplyCrateOverrideProperty = DependencyProperty.Register(nameof(SelectedSupplyCrateOverride), typeof(SupplyCrateOverride), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedSupplyCrateItemSetProperty = DependencyProperty.Register(nameof(SelectedSupplyCrateItemSet), typeof(SupplyCrateItemSet), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedSupplyCrateItemSetEntryProperty = DependencyProperty.Register(nameof(SelectedSupplyCrateItemSetEntry), typeof(SupplyCrateItemSetEntry), typeof(ServerSettingsControl));
        public static readonly DependencyProperty FilterOnlySelectedEngramsProperty = DependencyProperty.Register(nameof(FilterOnlySelectedEngrams), typeof(bool), typeof(ServerSettingsControl), new PropertyMetadata(false));
        public static readonly DependencyProperty ProcessPrioritiesProperty = DependencyProperty.Register(nameof(ProcessPriorities), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty CurrentCultureProperty = DependencyProperty.Register(nameof(CurrentCulture), typeof(CultureInfo), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty DisplayModInformationProperty = DependencyProperty.Register(nameof(DisplayModInformation), typeof(bool), typeof(ServerSettingsControl), new PropertyMetadata(false));
        public static readonly DependencyProperty CustomLevelProgressionsInformationProperty = DependencyProperty.Register(nameof(CustomLevelProgressionsInformation), typeof(string), typeof(ServerSettingsControl), new PropertyMetadata(""));
        public static readonly DependencyProperty ProfileLastStartedProperty = DependencyProperty.Register(nameof(ProfileLastStarted), typeof(string), typeof(ServerSettingsControl), new PropertyMetadata(""));
        public static readonly DependencyProperty DinoFilterStringProperty = DependencyProperty.Register(nameof(DinoFilterString), typeof(string), typeof(ServerSettingsControl), new PropertyMetadata(""));
        public static readonly DependencyProperty EngramFilterStringProperty = DependencyProperty.Register(nameof(EngramFilterString), typeof(string), typeof(ServerSettingsControl), new PropertyMetadata(""));
        public static readonly DependencyProperty ResourceFilterStringProperty = DependencyProperty.Register(nameof(ResourceFilterString), typeof(string), typeof(ServerSettingsControl), new PropertyMetadata(""));

        #region Properties
        public ComboBoxItemList BaseDinoModList
        {
            get { return (ComboBoxItemList)GetValue(BaseDinoModListProperty); }
            set { SetValue(BaseDinoModListProperty, value); }
        }

        public ComboBoxItemList BaseEngramModList
        {
            get { return (ComboBoxItemList)GetValue(BaseEngramModListProperty); }
            set { SetValue(BaseEngramModListProperty, value); }
        }

        public ComboBoxItemList BaseResourceModList
        {
            get { return (ComboBoxItemList)GetValue(BaseResourceModListProperty); }
            set { SetValue(BaseResourceModListProperty, value); }
        }

        public ComboBoxItemList BaseDinoList
        {
            get { return (ComboBoxItemList)GetValue(BaseDinoListProperty); }
            set { SetValue(BaseDinoListProperty, value); }
        }

        public ComboBoxItemList BaseMapSpawnerList
        {
            get { return (ComboBoxItemList)GetValue(BaseMapSpawnerListProperty); }
            set { SetValue(BaseMapSpawnerListProperty, value); }
        }

        public ComboBoxItemList BasePrimalItemList
        {
            get { return (ComboBoxItemList)GetValue(BasePrimalItemListProperty); }
            set { SetValue(BasePrimalItemListProperty, value); }
        }

        public ComboBoxItemList BaseSupplyCrateList
        {
            get { return (ComboBoxItemList)GetValue(BaseSupplyCrateListProperty); }
            set { SetValue(BaseSupplyCrateListProperty, value); }
        }

        public ComboBoxItemList BaseGameMaps
        {
            get { return (ComboBoxItemList)GetValue(BaseGameMapsProperty); }
            set { SetValue(BaseGameMapsProperty, value); }
        }

        public ComboBoxItemList BaseTotalConversions
        {
            get { return (ComboBoxItemList)GetValue(BaseTotalConversionsProperty); }
            set { SetValue(BaseTotalConversionsProperty, value); }
        }

        public ComboBoxItemList BaseBranches
        {
            get { return (ComboBoxItemList)GetValue(BaseBranchesProperty); }
            set { SetValue(BaseBranchesProperty, value); }
        }

        public ComboBoxItemList BaseEvents
        {
            get { return (ComboBoxItemList)GetValue(BaseEventsProperty); }
            set { SetValue(BaseEventsProperty, value); }
        }

        public bool BetaVersion
        {
            get { return (bool)GetValue(BetaVersionProperty); }
            set { SetValue(BetaVersionProperty, value); }
        }

        public Config Config
        {
            get { return GetValue(ConfigProperty) as Config; }
            set { SetValue(ConfigProperty, value); }
        }

        public ComboBoxItemList Culture
        {
            get { return (ComboBoxItemList)GetValue(CultureProperty); }
            set { SetValue(CultureProperty, value); }
        }

        public bool IsAdministrator
        {
            get { return (bool)GetValue(IsAdministratorProperty); }
            set { SetValue(IsAdministratorProperty, value); }
        }

        public List<NetworkAdapterEntry> NetworkInterfaces
        {
            get { return (List<NetworkAdapterEntry>)GetValue(NetworkInterfacesProperty); }
            set { SetValue(NetworkInterfacesProperty, value); }
        }

        public ServerRuntime Runtime
        {
            get { return GetValue(RuntimeProperty) as ServerRuntime; }
            set { SetValue(RuntimeProperty, value); }
        }

        public ServerManager ServerManager
        {
            get { return (ServerManager)GetValue(ServerManagerProperty); }
            set { SetValue(ServerManagerProperty, value); }
        }

        public Server Server
        {
            get { return (Server)GetValue(ServerProperty); }
            set { SetValue(ServerProperty, value); }
        }

        public ServerProfile Settings
        {
            get { return GetValue(SettingsProperty) as ServerProfile; }
            set { SetValue(SettingsProperty, value); }
        }

        public string SelectedModDino
        {
            get { return (string)GetValue(SelectedModDinoProperty); }
            set { SetValue(SelectedModDinoProperty, value); }
        }

        public string SelectedModEngram
        {
            get { return (string)GetValue(SelectedModEngramProperty); }
            set { SetValue(SelectedModEngramProperty, value); }
        }

        public string SelectedModResource
        {
            get { return (string)GetValue(SelectedModResourceProperty); }
            set { SetValue(SelectedModResourceProperty, value); }
        }

        public CraftingOverride SelectedCraftingOverride
        {
            get { return GetValue(SelectedCraftingOverrideProperty) as CraftingOverride; }
            set { SetValue(SelectedCraftingOverrideProperty, value); }
        }

        public CustomSection SelectedCustomEngineSetting
        {
            get { return GetValue(SelectedCustomEngineSettingProperty) as CustomSection; }
            set { SetValue(SelectedCustomEngineSettingProperty, value); }
        }

        public CustomSection SelectedCustomGameSetting
        {
            get { return GetValue(SelectedCustomGameSettingProperty) as CustomSection; }
            set { SetValue(SelectedCustomGameSettingProperty, value); }
        }

        public CustomSection SelectedCustomGameUserSetting
        {
            get { return GetValue(SelectedCustomGameUserSettingProperty) as CustomSection; }
            set { SetValue(SelectedCustomGameUserSettingProperty, value); }
        }

        public NPCSpawnSettings SelectedNPCSpawnSetting
        {
            get { return GetValue(SelectedNPCSpawnSettingProperty) as NPCSpawnSettings; }
            set { SetValue(SelectedNPCSpawnSettingProperty, value); }
        }

        public SupplyCrateOverride SelectedSupplyCrateOverride
        {
            get { return GetValue(SelectedSupplyCrateOverrideProperty) as SupplyCrateOverride; }
            set { SetValue(SelectedSupplyCrateOverrideProperty, value); }
        }

        public SupplyCrateItemSet SelectedSupplyCrateItemSet
        {
            get { return GetValue(SelectedSupplyCrateItemSetProperty) as SupplyCrateItemSet; }
            set { SetValue(SelectedSupplyCrateItemSetProperty, value); }
        }

        public SupplyCrateItemSetEntry SelectedSupplyCrateItemSetEntry
        {
            get { return GetValue(SelectedSupplyCrateItemSetEntryProperty) as SupplyCrateItemSetEntry; }
            set { SetValue(SelectedSupplyCrateItemSetEntryProperty, value); }
        }

        public bool FilterOnlySelectedEngrams
        {
            get { return (bool)GetValue(FilterOnlySelectedEngramsProperty); }
            set { SetValue(FilterOnlySelectedEngramsProperty, value); }
        }

        public ComboBoxItemList ProcessPriorities
        {
            get { return (ComboBoxItemList)GetValue(ProcessPrioritiesProperty); }
            set { SetValue(ProcessPrioritiesProperty, value); }
        }

        public CultureInfo CurrentCulture
        {
            get { return (CultureInfo)GetValue(CurrentCultureProperty); }
            set { SetValue(CurrentCultureProperty, value); }
        }

        public bool DisplayModInformation
        {
            get { return (bool)GetValue(DisplayModInformationProperty); }
            set { SetValue(DisplayModInformationProperty, value); }
        }

        public string CustomLevelProgressionsInformation
        {
            get { return (string)GetValue(CustomLevelProgressionsInformationProperty); }
            set { SetValue(CustomLevelProgressionsInformationProperty, value); }
        }

        public string ProfileLastStarted
        {
            get { return (string)GetValue(ProfileLastStartedProperty); }
            set { SetValue(ProfileLastStartedProperty, value); }
        }

        public string DinoFilterString
        {
            get { return (string)GetValue(DinoFilterStringProperty); }
            set { SetValue(DinoFilterStringProperty, value); }
        }

        public string EngramFilterString
        {
            get { return (string)GetValue(EngramFilterStringProperty); }
            set { SetValue(EngramFilterStringProperty, value); }
        }

        public string ResourceFilterString
        {
            get { return (string)GetValue(ResourceFilterStringProperty); }
            set { SetValue(ResourceFilterStringProperty, value); }
        }
        #endregion

        public ServerSettingsControl()
        {
            this.BetaVersion = App.Instance.BetaVersion;
            this.Config = Config.Default;
            this.CurrentCulture = Thread.CurrentThread.CurrentCulture;

            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.ServerManager = ServerManager.Instance;
            this.IsAdministrator = SecurityUtils.IsAdministrator();
            this.DisplayModInformation = !string.IsNullOrWhiteSpace(SteamUtils.SteamWebApiKey);

            RefreshBaseDinoModList();
            RefreshBaseEngramModList();
            RefreshBaseResourceModList();
            RefreshCustomLevelProgressionsInformation();

            this.BaseDinoList = new ComboBoxItemList();
            this.BaseMapSpawnerList = new ComboBoxItemList();
            this.BasePrimalItemList = new ComboBoxItemList();
            this.BaseSupplyCrateList = new ComboBoxItemList();
            this.BaseGameMaps = new ComboBoxItemList();
            this.BaseTotalConversions = new ComboBoxItemList();
            this.BaseBranches = new ComboBoxItemList();
            this.BaseEvents = new ComboBoxItemList();
            this.ProcessPriorities = new ComboBoxItemList();

            UpdateLastStartedDetails(false);

            // hook into the language change event
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent += ResourceDictionaryChangedEvent;
            GameData.GameDataLoaded += GameData_GameDataLoaded;
        }

        #region Event Methods
        private void GameData_GameDataLoaded(object sender, EventArgs e)
        {
            this.RefreshBaseDinoModList();
            this.RefreshBaseEngramModList();
            this.RefreshBaseResourceModList();

            this.RefreshBaseDinoList();
            this.RefreshBaseMapSpawnerList();
            this.RefreshBasePrimalItemList();
            this.RefreshBaseSupplyCrateList();
            this.RefreshBaseGameMapsList();
            this.RefreshBaseTotalConversionsList();
            this.RefreshCultureList();
            this.RefreshBaseBranchesList();
            this.RefreshBaseEventsList();
            this.RefreshProcessPrioritiesList();
            this.RefreshCustomLevelProgressionsInformation();

            this.HarvestResourceItemAmountClassMultipliersListBox.Items.Refresh();

            this.Settings.ConfigOverrideItemCraftingCosts.Update();
            this.Settings.ConfigOverrideItemMaxQuantity.Update();
            this.Settings.ConfigOverrideSupplyCrateItems.Update();
            this.Settings.ExcludeItemIndices.Update();
            this.Settings.NPCSpawnSettings.Update();
            this.Settings.PreventTransferForClassNames.Update();
        }

        private static void ServerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ssc = (ServerSettingsControl)d;
            var oldserver = (Server)e.OldValue;
            var server = (Server)e.NewValue;
            if (server != null)
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                    {
                        oldserver?.Profile.Save(false, false, null);

                        ssc.Settings = server.Profile;
                        ssc.Runtime = server.Runtime;
                        ssc.ReinitializeNetworkAdapters();
                        ssc.RefreshBaseDinoList();
                        ssc.RefreshBaseMapSpawnerList();
                        ssc.RefreshBasePrimalItemList();
                        ssc.RefreshBaseSupplyCrateList();
                        ssc.RefreshBaseGameMapsList();
                        ssc.RefreshBaseTotalConversionsList();
                        ssc.RefreshCultureList();
                        ssc.RefreshBaseBranchesList();
                        ssc.RefreshBaseEventsList();
                        ssc.RefreshProcessPrioritiesList();
                        ssc.DisplayModInformation = !string.IsNullOrWhiteSpace(SteamUtils.SteamWebApiKey);
                        ssc.UpdateLastStartedDetails(false);
                    }).DoNotWait();
            }
        }

        private void ResourceDictionaryChangedEvent(object source, ResourceDictionaryChangedEventArgs e)
        {
            this.CurrentCulture = Thread.CurrentThread.CurrentCulture;

            this.UpdateLastStartedDetails(false);
            GameData_GameDataLoaded(source, e);

            Runtime.UpdateServerStatusString();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Window.GetWindow(this)?.Activate();

            if (sender is Window)
                ((Window)sender).Closed -= Window_Closed;

            if (sender is ShutdownWindow)
                this.Runtime?.ResetModCheckTimer();

            if (sender is ModDetailsWindow)
            {
                ((ModDetailsWindow)sender).SavePerformed -= ModDetailsWindow_SavePerformed;
                RefreshBaseGameMapsList();
                RefreshBaseTotalConversionsList();
            }

            if (sender is FindSettingWindow)
            {
                _findSettingWindow = null;
                UnselectControl();
            }
        }

        private void ModDetailsWindow_SavePerformed(object sender, ProfileEventArgs e)
        {
            if (sender is ModDetailsWindow && Equals(e.Profile, Settings))
            {
                RefreshBaseGameMapsList();
                RefreshBaseTotalConversionsList();
            }
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBoxResult.None;

            switch (this.Runtime.Status)
            {
                case ServerStatus.Initializing:
                case ServerStatus.Running:
                    // check if the server is initialising.
                    if (this.Runtime.Status == ServerStatus.Initializing)
                    {
                        result = MessageBox.Show(_globalizer.GetResourceString("ServerSettings_StartServer_StartingLabel"), _globalizer.GetResourceString("ServerSettings_StartServer_StartingTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                            return;

                        try
                        {
                            PluginHelper.Instance.ProcessAlert(AlertType.Shutdown, this.Settings.ProfileName, Config.Default.Alert_ServerStopMessage);
                            await Task.Delay(2000);

                            await this.Server.StopAsync();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_StopServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        try
                        {
                            var shutdownWindow = ShutdownWindow.OpenShutdownWindow(this.Server);
                            if (shutdownWindow == null)
                            {
                                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ShutdownServer_AlreadyOpenLabel"), _globalizer.GetResourceString("ServerSettings_ShutdownServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            shutdownWindow.Owner = Window.GetWindow(this);
                            shutdownWindow.Closed += Window_Closed;
                            shutdownWindow.Show();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ShutdownServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    break;

                case ServerStatus.Stopped:
                    Mutex mutex = null;
                    bool createdNew = false;

                    try
                    {
                        // try to establish a mutex for the profile.
                        mutex = new Mutex(true, ServerApp.GetMutexName(this.Server.Profile.InstallDirectory), out createdNew);

                        // check if the mutex was established
                        if (createdNew)
                        {
                            if (Config.Default.ManagePublicIPAutomatically)
                            {
                                // check and update the public IP address
                                await App.DiscoverMachinePublicIPAsync(false);
                            }

                            this.Settings.Save(false, false, null);

                            if (Config.Default.ServerUpdate_OnServerStart)
                            {
                                if (!await UpdateServer(false, true, Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer, true))
                                {
                                    if (MessageBox.Show(_globalizer.GetResourceString("ServerUpdate_WarningLabel"), _globalizer.GetResourceString("ServerUpdate_Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                        return;
                                }
                            }

                            string validateMessage;
                            if (!this.Server.Profile.Validate(false, out validateMessage))
                            {
                                var outputMessage = _globalizer.GetResourceString("ProfileValidation_WarningLabel").Replace("{validateMessage}", validateMessage);
                                if (MessageBox.Show(outputMessage, _globalizer.GetResourceString("ProfileValidation_Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                    return;
                            }

                            await this.Server.StartAsync();

                            // update the profile's last started time
                            UpdateLastStartedDetails(true);

                            var startupMessage = Config.Default.Alert_ServerStartedMessage;
                            if (Config.Default.Alert_ServerStartedMessageIncludeIPandPort && !string.IsNullOrWhiteSpace(Config.Default.Alert_ServerStartedMessageIPandPort))
                            {
                                var ipAndPortMessage = Config.Default.Alert_ServerStartedMessageIPandPort
                                    .Replace("{ipaddress}", Config.Default.MachinePublicIP)
                                    .Replace("{port}", this.Settings.QueryPort.ToString());
                                startupMessage += $" {ipAndPortMessage}";
                            }
                            PluginHelper.Instance.ProcessAlert(AlertType.Startup, this.Settings.ProfileName, startupMessage);

                            if (this.Settings.ForceRespawnDinos)
                                PluginHelper.Instance.ProcessAlert(AlertType.Startup, this.Settings.ProfileName, Config.Default.Alert_ForceRespawnDinos);

                            await Task.Delay(2000);
                        }
                        else
                        {
                            // display an error message and exit
                            MessageBox.Show(_globalizer.GetResourceString("ServerSettings_StartServer_MutexFailedLabel"), _globalizer.GetResourceString("ServerSettings_StartServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_StartServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        if (mutex != null)
                        {
                            if (createdNew)
                            {
                                mutex.ReleaseMutex();
                                mutex.Dispose();
                            }
                            mutex = null;
                        }
                    }
                    break;
            }
        }

        private async void Upgrade_Click(object sender, RoutedEventArgs e)
        {
            switch (this.Runtime.Status)
            {
                case ServerStatus.Stopped:
                case ServerStatus.Uninstalled:
                    break;

                case ServerStatus.Running:
                case ServerStatus.Initializing:
                    var result = MessageBox.Show(_globalizer.GetResourceString("ServerSettings_UpgradeServer_RunningLabel"), _globalizer.GetResourceString("ServerSettings_UpgradeServer_RunningTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                        return;

                    break;

                case ServerStatus.Updating:
                    return;
            }

            this.Settings.Save(false, false, null);
            await UpdateServer(true, true, Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer, false);
        }

        private async void ModUpgrade_Click(object sender, RoutedEventArgs e)
        {
            switch (this.Runtime.Status)
            {
                case ServerStatus.Stopped:
                case ServerStatus.Uninstalled:
                    break;

                default:
                    return;
            }

            this.Settings.Save(false, false, null);
            await UpdateServer(true, false, true, false);
        }

        private void OpenRCON_Click(object sender, RoutedEventArgs e)
        {
            var window = RCONWindow.GetRCONForServer(this.Server);
            window.Closed += Window_Closed;
            window.Show();
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Focus();
        }

        private void OpenPlayerList_Click(object sender, RoutedEventArgs e)
        {
            var window = PlayerListWindow.GetWindowForServer(this.Server);
            window.Closed += Window_Closed;
            window.Show();
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Focus();
        }

        private void OpenModDetails_Click(object sender, RoutedEventArgs e)
        {
            var window = new ModDetailsWindow(this.Server.Profile);
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            window.SavePerformed += ModDetailsWindow_SavePerformed;
            window.Show();
            window.Focus();
        }

        private void PatchNotes_Click(object sender, RoutedEventArgs e)
        {
            var url = Settings.SOTF_Enabled ? Config.Default.AppPatchNotesUrlSotF : Config.Default.AppPatchNotesUrl;
            if (string.IsNullOrWhiteSpace(url))
                return;

            Process.Start(url);
        }

        private void NeedAdmin_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(_globalizer.GetResourceString("ServerSettings_AdminRequired_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_AdminRequired_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshLocalIPs_Click(object sender, RoutedEventArgs e)
        {
            ReinitializeNetworkAdapters();
        }

        private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            var logFolder = Path.Combine(App.GetLogFolder(), this.Server.Profile.ProfileID.ToLower());
            if (!Directory.Exists(logFolder))
                logFolder = App.GetLogFolder();
            if (!Directory.Exists(logFolder))
                logFolder = Config.Default.DataDir;
            Process.Start("explorer.exe", logFolder);
        }

        private void OpenServerFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", this.Server.Profile.InstallDirectory);
        }

        private async void CreateSupportZip_Click(object sender, RoutedEventArgs e)
        {
            const int MAX_DAYS = 2;

            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                var obfuscateFiles = new Dictionary<string, List<(string entryName, string contents)>>();
                var files = new List<string>();

                // <server>
                var file = Path.Combine(this.Settings.InstallDirectory, Config.Default.LastUpdatedTimeFile);
                if (File.Exists(file)) files.Add(file);

                file = Path.Combine(this.Settings.InstallDirectory, "version.txt");
                if (File.Exists(file)) files.Add(file);

                // <server>\ShooterGame\Content\Mods
                var folder = Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerModsRelativePath);
                var dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.mod").Select(modFile => modFile.FullName));
                    foreach (var modFolder in dirInfo.GetDirectories())
                    {
                        file = Path.Combine(modFolder.FullName, Config.Default.LastUpdatedTimeFile);
                        if (File.Exists(file)) files.Add(file);
                    }
                }

                // <server>\ShooterGame\Saved\Config\WindowsServer
                file = Path.Combine(this.Settings.GetProfileServerConfigDir(), Config.Default.ServerGameConfigFile);
                if (File.Exists(file))
                {
                    var iniFile = IniFileUtils.ReadFromFile(file);
                    if (iniFile != null)
                    {
                        var key = Path.GetDirectoryName(file);
                        var entryName = Path.GetFileName(file);

                        if (obfuscateFiles.ContainsKey(key))
                            obfuscateFiles[key].Add((entryName, iniFile.ToOutputString()));
                        else
                            obfuscateFiles.Add(key, new List<(string entryName, string contents)> 
                            { 
                                (entryName, iniFile.ToOutputString()) 
                            });
                    }
                }
                file = Path.Combine(this.Settings.GetProfileServerConfigDir(), Config.Default.ServerGameUserSettingsConfigFile);
                if (File.Exists(file))
                {
                    var iniFile = IniFileUtils.ReadFromFile(file);
                    if (iniFile != null)
                    {
                        iniFile.WriteKey("ServerSettings", "ServerPassword", "obfuscated");
                        iniFile.WriteKey("ServerSettings", "ServerAdminPassword", "obfuscated");
                        iniFile.WriteKey("ServerSettings", "SpectatorPassword", "obfuscated");

                        var key = Path.GetDirectoryName(file);
                        var entryName = Path.GetFileName(file);

                        if (obfuscateFiles.ContainsKey(key))
                            obfuscateFiles[key].Add((entryName, iniFile.ToOutputString()));
                        else
                            obfuscateFiles.Add(key, new List<(string entryName, string contents)>
                            {
                                (entryName, iniFile.ToOutputString())
                            });
                    }
                }
                file = Path.Combine(this.Settings.GetProfileServerConfigDir(), Config.Default.LauncherFile);
                if (File.Exists(file)) files.Add(file);

                // Logs
                folder = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, ServerApp.LOGPREFIX_AUTOBACKUP);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                folder = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, ServerApp.LOGPREFIX_AUTOSHUTDOWN);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                folder = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, ServerApp.LOGPREFIX_AUTOUPDATE);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                // Logs/<server>
                folder = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, this.Settings.ProfileID.ToLower());
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.*", SearchOption.AllDirectories).Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                // Profile
                file = this.Settings.GetProfileFile();
                if (File.Exists(file))
                {
                    var profileFile = ServerProfile.LoadFromProfileFile(file, null);
                    if (profileFile != null)
                    {
                        profileFile.AdminPassword = string.IsNullOrWhiteSpace(profileFile.AdminPassword) ? "empty" : "obfuscated";
                        profileFile.ServerPassword = string.IsNullOrWhiteSpace(profileFile.ServerPassword) ? "empty" : "obfuscated";
                        profileFile.SpectatorPassword = string.IsNullOrWhiteSpace(profileFile.SpectatorPassword) ? "empty" : "obfuscated";
                        profileFile.WebAlarmKey = string.IsNullOrWhiteSpace(profileFile.WebAlarmKey) ? "empty" : "obfuscated";
                        profileFile.WebAlarmUrl = string.IsNullOrWhiteSpace(profileFile.WebAlarmUrl) ? "empty" : "obfuscated";
                        profileFile.BranchPassword = string.IsNullOrWhiteSpace(profileFile.BranchPassword) ? "empty" : "obfuscated";

                        var key = Path.GetDirectoryName(file);
                        var entryName = Path.GetFileName(file);

                        if (obfuscateFiles.ContainsKey(key))
                            obfuscateFiles[key].Add((entryName, profileFile.ToOutputString()));
                        else
                            obfuscateFiles.Add(key, new List<(string entryName, string contents)>
                            {
                                (entryName, profileFile.ToOutputString())
                            });
                    }
                }

                // <data folder>\SteamCMD\steamapps\workshop\content\<app id>
                var appId = this.Settings.SOTF_Enabled ? Config.Default.AppId_SotF : Config.Default.AppId;
                var workshopPath = string.Format(Config.Default.AppSteamWorkshopFolderRelativePath, appId);
                folder = Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir, workshopPath);
                if (Directory.Exists(folder))
                {
                    foreach (var modFolder in Directory.GetDirectories(folder))
                    {
                        file = Path.Combine(modFolder, Config.Default.LastUpdatedTimeFile);
                        if (File.Exists(file)) files.Add(file);
                    }
                }

                if (!this.Settings.SOTF_Enabled)
                {
                    // <server cache>
                    if (!string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_CacheDir))
                    {
                        var branchName = string.IsNullOrWhiteSpace(this.Settings.BranchName) ? Config.Default.DefaultServerBranchName : this.Settings.BranchName;
                        file = IOUtils.NormalizePath(Path.Combine(Config.Default.AutoUpdate_CacheDir, $"{Config.Default.ServerBranchFolderPrefix}{branchName}", Config.Default.LastUpdatedTimeFile));
                        if (File.Exists(file)) files.Add(file);
                    }
                }

                // scheduled tasks (profile level)
                var taskKey = this.Settings.GetProfileKey();
                var taskList = new List<(string entryName, string contents)>();

                var taskXML = TaskSchedulerUtils.GetScheduleTaskInformation(TaskSchedulerUtils.TaskType.AutoStart, taskKey, null);
                if (!string.IsNullOrWhiteSpace(taskXML))
                {
                    taskList.Add(($"Task-{TaskSchedulerUtils.TaskType.AutoStart}.xml", taskXML));
                }

                taskXML = TaskSchedulerUtils.GetScheduleTaskInformation(TaskSchedulerUtils.TaskType.AutoShutdown, taskKey, "#1");
                if (!string.IsNullOrWhiteSpace(taskXML))
                {
                    taskList.Add(($"Task-{TaskSchedulerUtils.TaskType.AutoShutdown}-#1.xml", taskXML));
                }

                taskXML = TaskSchedulerUtils.GetScheduleTaskInformation(TaskSchedulerUtils.TaskType.AutoShutdown, taskKey, "#2");
                if (!string.IsNullOrWhiteSpace(taskXML))
                {
                    taskList.Add(($"Task-{TaskSchedulerUtils.TaskType.AutoShutdown}-#2.xml", taskXML));
                }

                // scheduled tasks (manager level)
                taskKey = TaskSchedulerUtils.ComputeKey(Config.Default.DataDir);

                taskXML = TaskSchedulerUtils.GetScheduleTaskInformation(TaskSchedulerUtils.TaskType.AutoBackup, taskKey, null);
                if (!string.IsNullOrWhiteSpace(taskXML))
                {
                    taskList.Add(($"Task-{TaskSchedulerUtils.TaskType.AutoBackup}.xml", taskXML));
                }

                taskXML = TaskSchedulerUtils.GetScheduleTaskInformation(TaskSchedulerUtils.TaskType.AutoUpdate, taskKey, null);
                if (!string.IsNullOrWhiteSpace(taskXML))
                {
                    taskList.Add(($"Task-{TaskSchedulerUtils.TaskType.AutoUpdate}.xml", taskXML));
                }

                if (obfuscateFiles.ContainsKey(""))
                    obfuscateFiles[""].AddRange(taskList);
                else
                    obfuscateFiles.Add("", taskList);

                // archive comment - mostly global config settings
                var comment = new StringBuilder();
                comment.AppendLine($"Windows Platform: {Environment.OSVersion.Platform}");
                comment.AppendLine($"Windows Version: {Environment.OSVersion.VersionString}");

                comment.AppendLine($"Game Server Version: {this.Settings.LastInstalledVersion}");
                comment.AppendLine($"Server Manager Version: {App.Instance.Version}");
                comment.AppendLine($"Server Manager Code: {Config.Default.ServerManagerCode}");
                comment.AppendLine($"Server Manager Key: {Config.Default.ServerManagerUniqueKey}");
                comment.AppendLine($"Server Manager Directory: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");

                comment.AppendLine($"MachinePublicIP: {Config.Default.MachinePublicIP}");
                comment.AppendLine($"Data Directory: {Config.Default.DataDir}");
                comment.AppendLine($"Profiles Directory: {Config.Default.ConfigDirectory}");
                comment.AppendLine($"Server Directory: {this.Settings.InstallDirectory}");

                comment.AppendLine($"SotF Server: {this.Settings.SOTF_Enabled}");
                comment.AppendLine($"PGM Server: {this.Settings.PGM_Enabled}");

                comment.AppendLine($"IsAdministrator: {SecurityUtils.IsAdministrator()}");
                comment.AppendLine($"RunAsAdministratorPrompt: {Config.Default.RunAsAdministratorPrompt}");
                comment.AppendLine($"CheckIfServerManagerRunningOnStartup: {Config.Default.CheckIfServerManagerRunningOnStartup}");
                comment.AppendLine($"ManageFirewallAutomatically: {Config.Default.ManageFirewallAutomatically}");
                comment.AppendLine($"ManagePublicIPAutomatically: {Config.Default.ManagePublicIPAutomatically}");
                comment.AppendLine($"MainWindow_WindowState: {Config.Default.MainWindow_WindowState}");
                comment.AppendLine($"MainWindow_MinimizeToTray: {Config.Default.MainWindow_MinimizeToTray}");
                comment.AppendLine($"ServerMonitorWindow_WindowState: {Config.Default.ServerMonitorWindow_WindowState}");

                comment.AppendLine($"SteamCMD File: {SteamCmdUpdater.GetSteamCmdFile(Config.Default.DataDir)}");
                comment.AppendLine($"SteamCmd_UseAnonymousCredentials: {Config.Default.SteamCmd_UseAnonymousCredentials}");
                comment.AppendLine($"SteamCmd_Username Set: {!string.IsNullOrWhiteSpace(Config.Default.SteamCmd_Username)}");
                comment.AppendLine($"SteamCmd_Password Set: {!string.IsNullOrWhiteSpace(Config.Default.SteamCmd_Password)}");
                comment.AppendLine($"SteamAPIKey: {!string.IsNullOrWhiteSpace(CommonConfig.Default.SteamAPIKey)}");
                comment.AppendLine($"SteamCmdIgnoreExitStatusCodes: {!string.IsNullOrWhiteSpace(Config.Default.SteamCmdIgnoreExitStatusCodes)}");

                comment.AppendLine($"SectionCraftingOverridesEnabled: {Config.Default.SectionCraftingOverridesEnabled}");
                comment.AppendLine($"SectionStackSizeOverridesEnabled: {Config.Default.SectionStackSizeOverridesEnabled}");
                comment.AppendLine($"SectionCustomEngineSettingsEnabled: {Config.Default.SectionCustomEngineSettingsEnabled}");
                comment.AppendLine($"SectionMapSpawnerOverridesEnabled: {Config.Default.SectionMapSpawnerOverridesEnabled}");
                comment.AppendLine($"SectionSupplyCrateOverridesEnabled: {Config.Default.SectionSupplyCrateOverridesEnabled}");
                comment.AppendLine($"SectionPreventTransferOverridesEnabled: {Config.Default.SectionPreventTransferOverridesEnabled}");
                comment.AppendLine($"SectionPGMEnabled: {Config.Default.SectionPGMEnabled}");
                comment.AppendLine($"SectionSOTFEnabled: {Config.Default.SectionSOTFEnabled}");

                comment.AppendLine($"CustomLevelXPIncrease_Player: {Config.Default.CustomLevelXPIncrease_Player}");
                comment.AppendLine($"CustomLevelXPIncrease_Dino: {Config.Default.CustomLevelXPIncrease_Dino}");

                comment.AppendLine($"ServerStatus_EnableActions: {Config.Default.ServerStatus_EnableActions}");
                comment.AppendLine($"ServerStatus_ShowActionConfirmation: {Config.Default.ServerStatus_ShowActionConfirmation}");

                comment.AppendLine($"ValidateProfileOnServerStart: {Config.Default.ValidateProfileOnServerStart}");
                comment.AppendLine($"ServerUpdate_OnServerStart: {Config.Default.ServerUpdate_OnServerStart}");
                comment.AppendLine($"ServerStartMinimized: {Config.Default.ServerStartMinimized}");

                comment.AppendLine($"ServerUpdate_UpdateModsWhenUpdatingServer: {Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer}");
                comment.AppendLine($"ServerUpdate_ForceUpdateMods: {Config.Default.ServerUpdate_ForceUpdateMods}");
                comment.AppendLine($"ServerUpdate_ForceCopyMods: {Config.Default.ServerUpdate_ForceCopyMods}");
                comment.AppendLine($"ServerUpdate_ForceUpdateModsIfNoSteamInfo: {Config.Default.ServerUpdate_ForceUpdateModsIfNoSteamInfo}");

                if (!string.IsNullOrWhiteSpace(Config.Default.BackupPath))
                    comment.AppendLine($"Backup Directory: {Config.Default.BackupPath}");
                else
                    comment.AppendLine($"Backup Directory: *{Path.Combine(Config.Default.DataDir, Config.Default.BackupDir)}");
                comment.AppendLine($"AutoBackup_IncludeSaveGamesFolder: {Config.Default.AutoBackup_IncludeSaveGamesFolder}");
                comment.AppendLine($"AutoBackup_DeleteOldFiles: {Config.Default.AutoBackup_DeleteOldFiles}");
                comment.AppendLine($"AutoBackup_DeleteInterval: {Config.Default.AutoBackup_DeleteInterval}");
                comment.AppendLine($"RCON_BackupMessageCommand: {Config.Default.RCON_BackupMessageCommand}");
                comment.AppendLine($"ServerBackup_WorldSaveMessage: {Config.Default.ServerBackup_WorldSaveMessage}");

                comment.AppendLine($"AutoBackup_EnableBackup: {Config.Default.AutoBackup_EnableBackup}");
                comment.AppendLine($"AutoBackup_BackupPeriod: {Config.Default.AutoBackup_BackupPeriod}");

                comment.AppendLine($"AutoUpdate_EnableUpdate: {Config.Default.AutoUpdate_EnableUpdate}");
                comment.AppendLine($"AutoUpdate_CacheDir: {Config.Default.AutoUpdate_CacheDir}");
                comment.AppendLine($"AutoUpdate_UpdatePeriod: {Config.Default.AutoUpdate_UpdatePeriod}");
                comment.AppendLine($"AutoUpdate_UseSmartCopy: {Config.Default.AutoUpdate_UseSmartCopy}");
                comment.AppendLine($"AutoUpdate_ValidateServerFiles: {Config.Default.AutoUpdate_ValidateServerFiles}");
                comment.AppendLine($"AutoUpdate_RetryOnFail: {Config.Default.AutoUpdate_RetryOnFail}");
                comment.AppendLine($"AutoUpdate_ParallelUpdate: {Config.Default.AutoUpdate_ParallelUpdate}");
                comment.AppendLine($"AutoUpdate_SequencialDelayPeriod: {Config.Default.AutoUpdate_SequencialDelayPeriod}");
                comment.AppendLine($"AutoUpdate_ShowUpdateReason: {Config.Default.AutoUpdate_ShowUpdateReason}");
                comment.AppendLine($"AutoUpdate_ShowUpdateReason: {Config.Default.AutoUpdate_UpdateReasonPrefix}");
                comment.AppendLine($"AutoUpdate_OverrideServerStartup: {Config.Default.AutoUpdate_OverrideServerStartup}");

                comment.AppendLine($"AutoRestart_EnabledGracePeriod: {Config.Default.AutoRestart_EnabledGracePeriod}");
                comment.AppendLine($"AutoRestart_GracePeriod: {Config.Default.AutoRestart_GracePeriod}");

                comment.AppendLine($"ServerShutdown_CheckForOnlinePlayers: {Config.Default.ServerShutdown_CheckForOnlinePlayers}");
                comment.AppendLine($"ServerShutdown_SendShutdownMessages: {Config.Default.ServerShutdown_SendShutdownMessages}");
                comment.AppendLine($"ServerShutdown_GracePeriod: {Config.Default.ServerShutdown_GracePeriod}");
                comment.AppendLine($"ServerShutdown_AllMessagesShowReason: {Config.Default.ServerShutdown_AllMessagesShowReason}");

                comment.AppendLine($"DiscordBotEnabled: {Config.Default.DiscordBotEnabled}");
                comment.AppendLine($"HasDiscordBotToken: {!string.IsNullOrWhiteSpace(Config.Default.DiscordBotToken)}");
                comment.AppendLine($"DiscordBotServerId: {Config.Default.DiscordBotServerId}");
                comment.AppendLine($"DiscordBotPrefix: {Config.Default.DiscordBotPrefix}");
                comment.AppendLine($"DiscordBotLogLevel: {Config.Default.DiscordBotLogLevel}");
                comment.AppendLine($"DiscordBotAllServersKeyword: {Config.Default.DiscordBotAllServersKeyword}");
                comment.AppendLine($"AllowDiscordBackup: {Config.Default.AllowDiscordBackup}");
                comment.AppendLine($"AllowDiscordRestart: {Config.Default.AllowDiscordRestart}");
                comment.AppendLine($"AllowDiscordShutdown: {Config.Default.AllowDiscordShutdown}");
                comment.AppendLine($"AllowDiscordStart: {Config.Default.AllowDiscordStart}");
                comment.AppendLine($"AllowDiscordStop: {Config.Default.AllowDiscordStop}");
                comment.AppendLine($"AllowDiscordUpdate: {Config.Default.AllowDiscordUpdate}");
                comment.AppendLine($"DiscordBotAllowAllBots: {Config.Default.DiscordBotAllowAllBots}");
                comment.AppendLine($"DiscordBotWhitelist: {string.Join(";", Config.Default.DiscordBotWhitelist)}");

                comment.AppendLine($"EmailNotify_AutoBackup: {Config.Default.EmailNotify_AutoBackup}");
                comment.AppendLine($"EmailNotify_AutoUpdate: {Config.Default.EmailNotify_AutoUpdate}");
                comment.AppendLine($"EmailNotify_AutoRestart: {Config.Default.EmailNotify_AutoRestart}");
                comment.AppendLine($"EmailNotify_ShutdownRestart: {Config.Default.EmailNotify_ShutdownRestart}");

                comment.AppendLine($"ServerShutdown_UseShutdownCommand: {Config.Default.ServerShutdown_UseShutdownCommand}");
                comment.AppendLine($"BackupWorldFile: {Config.Default.BackupWorldFile}");
                comment.AppendLine($"CloseShutdownWindowWhenFinished: {Config.Default.CloseShutdownWindowWhenFinished}");
                comment.AppendLine($"AutoUpdate_VerifyServerAfterUpdate: {Config.Default.AutoUpdate_VerifyServerAfterUpdate}");
                comment.AppendLine($"SteamCmdRemoveQuit: {CommonConfig.Default.SteamCmdRemoveQuit}");
                comment.AppendLine($"UpdateDirectoryPermissions: {Config.Default.UpdateDirectoryPermissions}");
                comment.AppendLine($"SteamCmdRedirectOutput: {Config.Default.SteamCmdRedirectOutput}");
                comment.AppendLine($"LoggingEnabled: {Config.Default.LoggingEnabled}");
                comment.AppendLine($"LoggingMaxArchiveDays: {Config.Default.LoggingMaxArchiveDays}");
                comment.AppendLine($"LoggingMaxArchiveFiles: {Config.Default.LoggingMaxArchiveFiles}");
                comment.AppendLine($"ServerShutdown_WorldSaveDelay: {Config.Default.ServerShutdown_WorldSaveDelay}");
                comment.AppendLine($"RCON_MessageCommand: {Config.Default.RCON_MessageCommand}");

                comment.AppendLine($"AutoBackup_TaskPriority: {Config.Default.AutoBackup_TaskPriority}");
                comment.AppendLine($"AutoUpdate_TaskPriority: {Config.Default.AutoUpdate_TaskPriority}");
                comment.AppendLine($"AutoShutdown_TaskPriority: {Config.Default.AutoShutdown_TaskPriority}");
                comment.AppendLine($"AutoStart_TaskPriority: {Config.Default.AutoStart_TaskPriority}");

                comment.AppendLine($"TaskSchedulerUsername: {Config.Default.TaskSchedulerUsername}");
                comment.AppendLine($"HasTaskSchedulerPassword: {!string.IsNullOrWhiteSpace(Config.Default.TaskSchedulerPassword)}");

                var zipFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), this.Settings.ProfileID + ".zip");
                if (File.Exists(zipFile)) File.Delete(zipFile);

                ZipUtils.ZipFiles(zipFile, files, comment.ToString());
                ZipUtils.ZipContents(zipFile, obfuscateFiles);

                var message = _globalizer.GetResourceString("ServerSettings_SupportZipSuccessLabel").Replace("{filename}", Path.GetFileName(zipFile));
                MessageBox.Show(message, _globalizer.GetResourceString("ServerSettings_SupportZipTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_SupportZipErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void ValidateProfile_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                string validationMessage;
                var result = this.Settings.Validate(true, out validationMessage);

                if (result)
                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ProfileValidateSuccessLabel"), _globalizer.GetResourceString("ServerSettings_ProfileValidateTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show(validationMessage, _globalizer.GetResourceString("ServerSettings_ProfileValidateTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ProfileValidateErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void SelectInstallDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = _globalizer.GetResourceString("ServerSettings_InstallServer_Title")
            };
            if (!string.IsNullOrWhiteSpace(Settings.InstallDirectory))
            {
                dialog.InitialDirectory = Settings.InstallDirectory;
            }

            var result = dialog.ShowDialog(Window.GetWindow(this));
            if (result == CommonFileDialogResult.Ok)
            {
                Settings.ServerMap = string.Empty;
                Settings.TotalConversionModId = string.Empty;

                Settings.ChangeInstallationFolder(dialog.FileName, reloadConfigFiles: true);
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Config.Default.ConfigDirectory))
            {
                Directory.CreateDirectory(Config.Default.ConfigDirectory);
            }

            var dialog = new CommonOpenFileDialog
            {
                EnsureFileExists = true,
                InitialDirectory = Config.Default.ConfigDirectory,
                Multiselect = false,
                Title = _globalizer.GetResourceString("ServerSettings_LoadConfig_Title")
            };
            dialog.Filters.Add(new CommonFileDialogFilter("Profile", Config.Default.LoadProfileExtensionList));

            if (dialog.ShowDialog(Window.GetWindow(this)) == CommonFileDialogResult.Ok)
            {
                try
                {
                    this.Server.ImportFromPath(dialog.FileName, Settings);
                    this.Server.Profile.ResetProfileId();

                    this.Settings = this.Server.Profile;
                    this.Runtime = this.Server.Runtime;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format(_globalizer.GetResourceString("ServerSettings_LoadConfig_ErrorLabel"), dialog.FileName, ex.Message, ex.StackTrace), _globalizer.GetResourceString("ServerSettings_LoadConfig_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void ShowCmd_Click(object sender, RoutedEventArgs e)
        {
            var cmdLine = new CommandLineWindow(String.Format("{0} {1}", this.Runtime.GetServerExe(), this.Settings.GetServerArgs()));
            cmdLine.Owner = Window.GetWindow(this);
            cmdLine.ShowDialog();
        }

        private void ArkAutoSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ArkAutoSettings_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_ArkAutoSettings_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ResetServer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ResetServer_ConfirmationLabel"), _globalizer.GetResourceString("ServerSettings_ResetServer_Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);

                await this.Server.ResetAsync();

                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ResetServer_SuccessfulLabel"), _globalizer.GetResourceString("ServerSettings_ResetServer_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ResetServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = null);
            }
        }

        private async void SaveBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);

                var app = new ServerApp(true)
                {
                    DeleteOldBackupFiles = !Config.Default.AutoBackup_EnableBackup,
                    SendEmails = false,
                    OutputLogs = false,
                    ServerProcess = ServerProcessType.Backup,
                };

                var profile = ServerProfileSnapshot.Create(Server.Profile);

                var exitCode = await Task.Run(() => app.PerformProfileBackup(profile, CancellationToken.None));
                if (exitCode != ServerApp.EXITCODE_NORMALEXIT && exitCode != ServerApp.EXITCODE_CANCELLED)
                    throw new ApplicationException($"An error occured during the backup process - ExitCode: {exitCode}");

                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_BackupServer_SuccessfulLabel"), _globalizer.GetResourceString("ServerSettings_BackupServer_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_BackupServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = null);
            }
        }

        private void SaveRestore_Click(object sender, RoutedEventArgs e)
        {
            var window = new WorldSaveRestoreWindow(Server.Profile);
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            window.ShowDialog();
        }

        private void SettingFind_Click(object sender, RoutedEventArgs e)
        {
            if (_findSettingWindow is null)
            {
                _findSettingWindow = new FindSettingWindow(this);
                _findSettingWindow.Owner = Window.GetWindow(this);
                _findSettingWindow.Closed += Window_Closed;
            }
            _findSettingWindow.Show();
            _findSettingWindow.Focus();
        }

        private void HiddenField_GotFocus(object sender, RoutedEventArgs e)
        {
            var hideTextBox = sender as TextBox;
            if (hideTextBox != null)
            {
                TextBox textBox = null;
                if (Equals(hideTextBox, HideServerPasswordTextBox)) 
                    textBox = ServerPasswordTextBox;
                if (Equals(hideTextBox, HideAdminPasswordTextBox))
                    textBox = AdminPasswordTextBox;
                if (Equals(hideTextBox, HideSpectatorPasswordTextBox))
                    textBox = SpectatorPasswordTextBox;
                if (Equals(hideTextBox, HideWebKeyTextBox))
                    textBox = WebKeyTextBox;
                if (Equals(hideTextBox, HideWebURLTextBox))
                    textBox = WebURLTextBox;
                if (Equals(hideTextBox, HideBranchPasswordTextBox)) 
                    textBox = BranchPasswordTextBox;

                if (textBox != null)
                {
                    textBox.Visibility = System.Windows.Visibility.Visible;
                    hideTextBox.Visibility = System.Windows.Visibility.Collapsed;
                    textBox.Focus();
                }

                UpdateLayout();
            }
        }

        private void HiddenField_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                TextBox hideTextBox = null;
                if (textBox == ServerPasswordTextBox)
                    hideTextBox = HideServerPasswordTextBox;
                if (textBox == AdminPasswordTextBox)
                    hideTextBox = HideAdminPasswordTextBox;
                if (textBox == SpectatorPasswordTextBox)
                    hideTextBox = HideSpectatorPasswordTextBox;
                if (textBox == WebKeyTextBox)
                    hideTextBox = HideWebKeyTextBox;
                if (textBox == WebURLTextBox)
                    hideTextBox = HideWebURLTextBox;
                if (textBox == BranchPasswordTextBox)
                    hideTextBox = HideBranchPasswordTextBox;

                if (hideTextBox != null)
                {
                    hideTextBox.Visibility = System.Windows.Visibility.Visible;
                    textBox.Visibility = System.Windows.Visibility.Collapsed;
                }
                UpdateLayout();
            }
        }

        private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            if (comboBox.IsDropDownOpen)
                return;

            e.Handled = true;
        }

        private void ComboBoxItemList_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            if (comboBox.SelectedItem == null)
            {
                var text = comboBox.Text;

                var source = comboBox.ItemsSource as ComboBoxItemList;
                source?.Add(new Common.Model.ComboBoxItem
                {
                    ValueMember = text,
                    DisplayMember = text,
                });

                comboBox.SelectedValue = text;
            }

            var expression = comboBox.GetBindingExpression(Selector.SelectedValueProperty);
            expression?.UpdateSource();

            expression = comboBox.GetBindingExpression(ComboBox.TextProperty);
            expression?.UpdateSource();
        }

        private void OutOfDateModUpdate_Click(object sender, RoutedEventArgs e)
        {
            this.Runtime?.ResetModCheckTimer();
        }

        private void ProfileName_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.UpdateProfileToolTip();
        }

        private void ServerName_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.ValidateServerName();
        }

        private void Ports_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            // force the property to be updated.
            Settings.ServerPort = Settings.ServerPort;
            Settings.UpdatePortsString();
        }

        private void MOTD_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.ValidateMOTD();
        }

        private void SyncProfile_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProfileSyncWindow(ServerManager, Server.Profile);
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            window.ShowDialog();
        }

        private void EnableSOTFCheckbox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            var checkBox = sender as CheckBoxAndTextBlock;
            if (checkBox == null || checkBox != EnableSOTFCheckbox)
                return;

            this.Settings.ServerMap = string.Empty;
            this.Settings.TotalConversionModId = string.Empty;
            this.Settings.ServerModIds = string.Empty;
            this.Settings.BranchName = string.Empty;
            this.Settings.BranchPassword = string.Empty;
            this.Settings.EventName = string.Empty;

            RefreshBaseGameMapsList();
            RefreshBaseTotalConversionsList();
            RefreshBaseBranchesList();
            RefreshBaseEventsList();
        }

        private void OpenAffinity_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProcessorAffinityWindow(Server.Profile.ProfileName, Server.Profile.ProcessAffinity)
            {
                Owner = Window.GetWindow(this)
            };
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Server.Profile.ProcessAffinity = window.AffinityValue;
            }
        }

        private void SupplyCratesGrids_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void ExcludeItemIndicesOverrideGrids_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.ExcludeItemIndices.Update();
        }

        private void CraftingOverrideGrids_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.ConfigOverrideItemCraftingCosts.Update();
        }

        private void PreventTransferOverrideGrids_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.PreventTransferForClassNames.Update();
        }

        private void NPCSpawnSettingsGrids_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.NPCSpawnSettings.Update();
        }

        private void StackSizeOverrideGrids_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.ConfigOverrideItemMaxQuantity.Update();
        }

        #region Dinos
        private void DinoCustomization_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoCustomization_ResetLabel"), _globalizer.GetResourceString("ServerSettings_DinoCustomization_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.DinoSettings.Reset();
            RefreshBaseDinoList();
        }

        private void DinoMod_OnFilter(object sender, FilterEventArgs e)
        {
            e.Accepted = FilterInDino(e.Item as DinoSettings);
        }

        private void FilterDino_Click(object sender, RoutedEventArgs e)
        {
            var view = this.DinoSettingsGrid.ItemsSource as ListCollectionView;
            view?.Refresh();
        }

        private void PasteCustomDinos_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            Server.Profile.DinoSettings.RenderToModel();

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                var dinoSpawnWeightMultipliers = new AggregateIniValueList<DinoSpawn>(nameof(Server.Profile.DinoSpawnWeightMultipliers), null);
                dinoSpawnWeightMultipliers.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{dinoSpawnWeightMultipliers.IniCollectionKey}=")));
                Server.Profile.DinoSpawnWeightMultipliers.AddRange(dinoSpawnWeightMultipliers);
                Server.Profile.DinoSpawnWeightMultipliers.IsEnabled |= dinoSpawnWeightMultipliers.IsEnabled;

                var preventDinoTameClassNames = new StringIniValueList(nameof(Server.Profile.PreventDinoTameClassNames), null);
                preventDinoTameClassNames.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{preventDinoTameClassNames.IniCollectionKey}=")));
                Server.Profile.PreventDinoTameClassNames.AddRange(preventDinoTameClassNames);
                Server.Profile.PreventDinoTameClassNames.IsEnabled |= preventDinoTameClassNames.IsEnabled;

                var preventBreedingForClassNames = new StringIniValueList(nameof(Server.Profile.PreventBreedingForClassNames), null);
                preventBreedingForClassNames.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{preventBreedingForClassNames.IniCollectionKey}=")));
                Server.Profile.PreventBreedingForClassNames.AddRange(preventBreedingForClassNames);
                Server.Profile.PreventBreedingForClassNames.IsEnabled |= preventBreedingForClassNames.IsEnabled;

                var npcReplacements = new AggregateIniValueList<NPCReplacement>(nameof(Server.Profile.NPCReplacements), null);
                npcReplacements.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{npcReplacements.IniCollectionKey}=")));
                Server.Profile.NPCReplacements.AddRange(npcReplacements);
                Server.Profile.NPCReplacements.IsEnabled |= npcReplacements.IsEnabled;

                var tamedDinoClassDamageMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(Server.Profile.TamedDinoClassDamageMultipliers), null);
                tamedDinoClassDamageMultipliers.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{tamedDinoClassDamageMultipliers.IniCollectionKey}=")));
                Server.Profile.TamedDinoClassDamageMultipliers.AddRange(tamedDinoClassDamageMultipliers);
                Server.Profile.TamedDinoClassDamageMultipliers.IsEnabled |= tamedDinoClassDamageMultipliers.IsEnabled;

                var tamedDinoClassResistanceMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(Server.Profile.TamedDinoClassResistanceMultipliers), null);
                tamedDinoClassResistanceMultipliers.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{tamedDinoClassResistanceMultipliers.IniCollectionKey}=")));
                Server.Profile.TamedDinoClassResistanceMultipliers.AddRange(tamedDinoClassResistanceMultipliers);
                Server.Profile.TamedDinoClassResistanceMultipliers.IsEnabled |= tamedDinoClassResistanceMultipliers.IsEnabled;

                var dinoClassDamageMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(Server.Profile.DinoClassDamageMultipliers), null);
                dinoClassDamageMultipliers.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{dinoClassDamageMultipliers.IniCollectionKey}=")));
                Server.Profile.DinoClassDamageMultipliers.AddRange(dinoClassDamageMultipliers);
                Server.Profile.DinoClassDamageMultipliers.IsEnabled |= dinoClassDamageMultipliers.IsEnabled;

                var dinoClassResistanceMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(Server.Profile.DinoClassResistanceMultipliers), null);
                dinoClassResistanceMultipliers.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{dinoClassResistanceMultipliers.IniCollectionKey}=")));
                Server.Profile.DinoClassResistanceMultipliers.AddRange(dinoClassResistanceMultipliers);
                Server.Profile.DinoClassResistanceMultipliers.IsEnabled |= dinoClassResistanceMultipliers.IsEnabled;
            }

            Server.Profile.DinoSettings = new DinoSettingsList(Server.Profile.DinoSpawnWeightMultipliers, Server.Profile.PreventDinoTameClassNames, Server.Profile.PreventBreedingForClassNames, Server.Profile.NPCReplacements, Server.Profile.TamedDinoClassDamageMultipliers, Server.Profile.TamedDinoClassResistanceMultipliers, Server.Profile.DinoClassDamageMultipliers, Server.Profile.DinoClassResistanceMultipliers);
            Server.Profile.DinoSettings.RenderToView();

            RefreshBaseDinoList();
        }

        private void RemoveDinoSetting_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoCustomization_DinoRemoveRecordLabel"), _globalizer.GetResourceString("ServerSettings_DinoCustomization_DinoRemoveRecordTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var dino = ((DinoSettings)((Button)e.Source).DataContext);
            if (!dino.KnownDino)
            {
                this.Settings.DinoSettings.Remove(dino);
                RefreshBaseDinoList();
            }
        }

        private void SaveCustomDinos_Click(object sender, RoutedEventArgs e)
        {
            Settings.DinoSettings.RenderToModel();

            var iniValues = new List<string>();
            iniValues.AddRange(Settings.DinoSpawnWeightMultipliers.ToIniValues());
            iniValues.AddRange(Settings.PreventDinoTameClassNames.ToIniValues());
            iniValues.AddRange(Settings.PreventBreedingForClassNames.ToIniValues());
            iniValues.AddRange(Settings.NPCReplacements.ToIniValues());
            iniValues.AddRange(Settings.DinoClassDamageMultipliers.ToIniValues());
            iniValues.AddRange(Settings.DinoClassResistanceMultipliers.ToIniValues());
            iniValues.AddRange(Settings.TamedDinoClassDamageMultipliers.ToIniValues());
            iniValues.AddRange(Settings.TamedDinoClassResistanceMultipliers.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_DinoCustomizations_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private bool FilterInDino(DinoSettings dino)
        {
            if (dino == null)
                return false;

            return (SelectedModDino == GameData.MOD_ALL || dino.Mod == SelectedModDino) && (string.IsNullOrWhiteSpace(DinoFilterString) || dino.DisplayName.ToLower().Contains(DinoFilterString.ToLower()));
        }
        #endregion

        #region Resources
        private void HarvestResourceItemAmountClassMultipliers_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_CustomHarvest_ResetLabel"), _globalizer.GetResourceString("ServerSettings_CustomHarvest_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.HarvestResourceItemAmountClassMultipliers.Reset();
        }

        private void PasteCustomResources_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                var harvestResourceItemAmountClassMultipliers = new AggregateIniValueList<ResourceClassMultiplier>(nameof(Server.Profile.HarvestResourceItemAmountClassMultipliers), null);
                harvestResourceItemAmountClassMultipliers.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{harvestResourceItemAmountClassMultipliers.IniCollectionKey}=")));
                Server.Profile.HarvestResourceItemAmountClassMultipliers.AddRange(harvestResourceItemAmountClassMultipliers);
                Server.Profile.HarvestResourceItemAmountClassMultipliers.IsEnabled |= harvestResourceItemAmountClassMultipliers.IsEnabled;
            }
        }

        private void RemoveHarvestResource_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Harvest_HarvestRemoveRecordLabel"), _globalizer.GetResourceString("ServerSettings_Harvest_HarvestRemoveRecordTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var resource = ((ResourceClassMultiplier)((Button)e.Source).DataContext);
            if (!resource.KnownResource)
                this.Settings.HarvestResourceItemAmountClassMultipliers.Remove(resource);
        }

        private void ResourceMod_OnFilter(object sender, FilterEventArgs e)
        {
            e.Accepted = FilterInResource(e.Item as ResourceClassMultiplier);
        }

        private void FilterResource_Click(object sender, RoutedEventArgs e)
        {
            var view = this.HarvestResourceItemAmountClassMultipliersListBox.ItemsSource as ListCollectionView;
            view?.Refresh();
        }

        private void SaveCustomResources_Click(object sender, RoutedEventArgs e)
        {
            var iniValues = Settings.HarvestResourceItemAmountClassMultipliers.ToIniValues();
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_CustomResources_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private bool FilterInResource(ResourceClassMultiplier resource)
        {
            if (resource == null)
                return false;

            return (SelectedModResource == GameData.MOD_ALL || resource.Mod == SelectedModResource) && (string.IsNullOrWhiteSpace(ResourceFilterString) || resource.DisplayName.ToLower().Contains(ResourceFilterString.ToLower()));
        }
        #endregion

        #region Engrams
        private void Engrams_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_ResetLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetEngramsSection();
        }

        private void Engrams_SelectAll(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_SelectAllConfirmLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_SelectAllConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            foreach (var engram in Settings.EngramSettings)
            {
                if (FilterInEngram(engram))
                    engram.SaveEngramOverride = true;
            }
        }

        private void Engrams_UnselectAll(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_UnselectAllConfirmLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_UnselectAllConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            foreach (var engram in Settings.EngramSettings)
            {
                if (FilterInEngram(engram))
                    engram.SaveEngramOverride = false;
            }
        }

        private void EngramMod_OnFilter(object sender, FilterEventArgs e)
        {
            e.Accepted = FilterInEngram(e.Item as EngramSettings);
        }

        private async void ExportCustomEngrams_Click(object sender, RoutedEventArgs e)
        {
            // ask user for a filename to save the engram data to
            var dialog = new CommonSaveFileDialog
            {
                Title = GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ExportDialogTitle"),
                DefaultExtension = GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ExportDefaultExtension")
            };
            dialog.Filters.Add(new CommonFileDialogFilter(GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ExportFilterLabel"), GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ExportFilterExtension")));

            if (dialog == null || dialog.ShowDialog(Window.GetWindow(this)) != CommonFileDialogResult.Ok)
                return;

            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                // update the data export
                Settings.EngramSettings.OnlyAllowSpecifiedEngrams = Settings.OnlyAllowSpecifiedEngrams;

                // perform the json serialization
                Common.Utils.JsonUtils.SerializeToFile(new
                {
                    Settings.EngramSettings.OnlyAllowSpecifiedEngrams,
                    EngramSettings = Settings.EngramSettings.ToList(),
                }, dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ExportErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void FilterEngram_Click(object sender, RoutedEventArgs e)
        {
            var view = this.EngramsOverrideGrid.ItemsSource as ListCollectionView;
            view?.Refresh();
        }

        private async void ImportCustomEngrams_Click(object sender, RoutedEventArgs e)
        {
            // ask user for a filename to load the engram data from
            var dialog = new CommonOpenFileDialog
            {
                Title = GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ImportDialogTitle"),
                DefaultExtension = GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ImportDefaultExtension")
            };
            dialog.Filters.Add(new CommonFileDialogFilter(GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ImportFilterLabel"), GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ImportFilterExtension")));

            if (dialog == null || dialog.ShowDialog(Window.GetWindow(this)) != CommonFileDialogResult.Ok)
                return;

            // confirm with user which option to use - Overwrite, Merge or Cancel
            var result = MessageBox.Show(GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ImportConfirmationLabel"), GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ImportConfirmationTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                // perform the json deserialization
                var input = Common.Utils.JsonUtils.DeserializeFromFile(dialog.FileName, new
                {
                    OnlyAllowSpecifiedEngrams = false,
                    EngramSettings = new List<EngramSettings>(),
                });

                if (input?.EngramSettings != null)
                {
                    // perform the class population
                    Settings.OnlyAllowSpecifiedEngrams = input.OnlyAllowSpecifiedEngrams;
                    Settings.EngramSettings.OnlyAllowSpecifiedEngrams = input.OnlyAllowSpecifiedEngrams;

                    foreach (var engramSetting in input.EngramSettings.Where(engram => !string.IsNullOrWhiteSpace(engram.EngramClassName)))
                    {
                        var foundEngramSetting = Settings.EngramSettings.FirstOrDefault(engram => engram.EngramClassName.Equals(engramSetting.EngramClassName, StringComparison.OrdinalIgnoreCase));
                        if (foundEngramSetting == null)
                        {
                            // engram not found, add to the list
                            Settings.EngramSettings.Add(engramSetting);
                        }
                        else
                        {
                            // engram was found, update the values
                            foundEngramSetting.Mod = engramSetting.Mod;
                            foundEngramSetting.IsTekgram = engramSetting.IsTekgram;
                            foundEngramSetting.EngramLevelRequirement = engramSetting.EngramLevelRequirement;
                            foundEngramSetting.EngramPointsCost = engramSetting.EngramPointsCost;
                            foundEngramSetting.EngramHidden = engramSetting.EngramHidden;
                            foundEngramSetting.RemoveEngramPreReq = engramSetting.RemoveEngramPreReq;
                            foundEngramSetting.EngramAutoUnlock = engramSetting.EngramAutoUnlock;
                            foundEngramSetting.LevelToAutoUnlock = engramSetting.LevelToAutoUnlock;
                            foundEngramSetting.SaveEngramOverride = engramSetting.SaveEngramOverride;
                        }
                    }

                    // perform a model reload
                    Settings.EngramSettings.RenderToModel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, GlobalizedApplication.Instance.GetResourceString("ServerSettings_EngramsOverride_ImportErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void PasteCustomEngrams_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            Server.Profile.EngramSettings.OnlyAllowSpecifiedEngrams = Server.Profile.OnlyAllowSpecifiedEngrams;
            Server.Profile.EngramSettings.RenderToModel();

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                var overrideNamedEngramEntries = new EngramEntryList(nameof(Server.Profile.OverrideNamedEngramEntries));
                overrideNamedEngramEntries.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{overrideNamedEngramEntries.IniCollectionKey}=")));
                Server.Profile.OverrideNamedEngramEntries.AddRange(overrideNamedEngramEntries);
                Server.Profile.OverrideNamedEngramEntries.IsEnabled |= overrideNamedEngramEntries.IsEnabled;

                var engramEntryAutoUnlocks = new EngramAutoUnlockList(nameof(Server.Profile.EngramEntryAutoUnlocks));
                engramEntryAutoUnlocks.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{engramEntryAutoUnlocks.IniCollectionKey}=")));
                Server.Profile.EngramEntryAutoUnlocks.AddRange(engramEntryAutoUnlocks);
                Server.Profile.EngramEntryAutoUnlocks.IsEnabled |= engramEntryAutoUnlocks.IsEnabled;
            }

            Server.Profile.EngramSettings = new EngramSettingsList(Server.Profile.OverrideNamedEngramEntries, Server.Profile.EngramEntryAutoUnlocks);
            Server.Profile.EngramSettings.OnlyAllowSpecifiedEngrams = Server.Profile.OnlyAllowSpecifiedEngrams;
            Server.Profile.EngramSettings.RenderToView();
        }

        private void RemoveEngramOverride_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_EngramsRemoveRecordLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_EngramsRemoveRecordTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var engram = ((EngramSettings)((Button)e.Source).DataContext);
            if (!engram.KnownEngram)
                this.Settings.EngramSettings.Remove(engram);
        }

        private void SaveCustomEngrams_Click(object sender, RoutedEventArgs e)
        {
            Settings.EngramSettings.OnlyAllowSpecifiedEngrams = Settings.OnlyAllowSpecifiedEngrams;
            Settings.EngramSettings.RenderToModel();

            var iniValues = new List<string>();
            iniValues.AddRange(Settings.OverrideNamedEngramEntries.ToIniValues());
            iniValues.AddRange(Settings.EngramEntryAutoUnlocks.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_CustomEngrams_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private bool FilterInEngram(EngramSettings engram)
        {
            if (engram == null)
                return false;

            return (SelectedModEngram == GameData.MOD_ALL || engram.Mod == SelectedModEngram) && (!Settings.OnlyAllowSpecifiedEngrams || !FilterOnlySelectedEngrams || engram.SaveEngramOverride) && (string.IsNullOrWhiteSpace(EngramFilterString) || engram.DisplayName.ToLower().Contains(EngramFilterString.ToLower()));
        }
        #endregion

        #region Crafting Overrides
        private void AddCraftingOverride_Click(object sender, RoutedEventArgs e)
        {
            Settings.ConfigOverrideItemCraftingCosts.Add(new CraftingOverride());
            Settings.ConfigOverrideItemCraftingCosts.Update();
        }

        private void AddCraftingOverrideResource_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCraftingOverride == null)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_AddChildErrorLabel"), _globalizer.GetResourceString("ServerSettings_AddChildErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SelectedCraftingOverride.BaseCraftingResourceRequirements.Add(new CraftingResourceRequirement());
            Settings.ConfigOverrideItemCraftingCosts.Update();
        }

        private void ClearCraftingOverrides_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCraftingOverride = null;
            Settings.ConfigOverrideItemCraftingCosts.Clear();
            Settings.ConfigOverrideItemCraftingCosts.Update();
        }

        private void ClearCraftingOverrideResources_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCraftingOverride?.BaseCraftingResourceRequirements.Clear();
            Settings.ConfigOverrideItemCraftingCosts.Update();
        }

        private void PasteCraftingOverride_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData.Replace(" ", ""));

            Server.Profile.ConfigOverrideItemCraftingCosts.RenderToModel();

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                var configOverrideItemCraftingCosts = new AggregateIniValueList<CraftingOverride>(nameof(Server.Profile.ConfigOverrideItemCraftingCosts), null);
                configOverrideItemCraftingCosts.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{configOverrideItemCraftingCosts.IniCollectionKey}=")));
                Server.Profile.ConfigOverrideItemCraftingCosts.AddRange(configOverrideItemCraftingCosts);
                Server.Profile.ConfigOverrideItemCraftingCosts.IsEnabled |= configOverrideItemCraftingCosts.IsEnabled;
            }

            var errors = Server.Profile.ConfigOverrideItemCraftingCosts.RenderToView();

            RefreshBasePrimalItemList();
        }

        private void RemoveCraftingOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((CraftingOverride)((Button)e.Source).DataContext);
            Settings.ConfigOverrideItemCraftingCosts.Remove(item);
            Settings.ConfigOverrideItemCraftingCosts.Update();
        }

        private void RemoveCraftingOverrideResource_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCraftingOverride == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((CraftingResourceRequirement)((Button)e.Source).DataContext);
            SelectedCraftingOverride.BaseCraftingResourceRequirements.Remove(item);
            Settings.ConfigOverrideItemCraftingCosts.Update();
        }

        private void SaveCraftingOverride_Click(object sender, RoutedEventArgs e)
        {
            Settings.ConfigOverrideItemCraftingCosts.RenderToModel();

            var iniValues = new List<string>();
            iniValues.AddRange(Settings.ConfigOverrideItemCraftingCosts.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_CraftingOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void SaveCraftingOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            var item = ((CraftingOverride)((Button)e.Source).DataContext);
            if (item == null)
                return;

            Settings.ConfigOverrideItemCraftingCosts.RenderToModel();

            var iniName = Settings.ConfigOverrideItemCraftingCosts.IniCollectionKey;
            var iniValue = $"{iniName}={item.ToINIValue()}";

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_CraftingOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Custom GameUserSettings
        private void AddCustomGameUserSettingItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedCustomGameUserSetting?.Add(string.Empty, string.Empty);
        }

        private void AddCustomGameUserSettingSection_Click(object sender, RoutedEventArgs e)
        {
            Settings.CustomGameUserSettings.Add(string.Empty, new string[0]);
        }

        private void ClearCustomGameUserSettingItems_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCustomGameUserSetting?.Clear();
        }

        private void ClearCustomGameUserSettingSections_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCustomGameUserSetting = null;
            Settings.CustomGameUserSettings.Clear();
        }

        private void ImportCustomGameUserSettingSections_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.EnsureFileExists = true;
            dialog.Multiselect = false;
            dialog.Title = _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Title");
            dialog.Filters.Add(new CommonFileDialogFilter("Ini Files", "*.ini"));
            dialog.InitialDirectory = Settings.InstallDirectory;
            var result = dialog.ShowDialog(Window.GetWindow(this));
            if (result == CommonFileDialogResult.Ok)
            {
                try
                {
                    // read the selected ini file.
                    var iniFile = IniFileUtils.ReadFromFile(dialog.FileName);

                    // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
                    foreach (var section in iniFile.Sections.Where(s => !string.IsNullOrWhiteSpace(s.SectionName) && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
                    {
                        Settings.CustomGameUserSettings.Add(section.SectionName, section.KeysToStringEnumerable(), false);
                    }

                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Label"), _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void PasteCustomGameUserSettingItems_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCustomGameUserSetting == null)
                return;

            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);
            // get the section with the same name as the currently selected custom section.
            var section = iniFile?.GetSection(SelectedCustomGameUserSetting.SectionName);
            // check if the section exists.
            if (section == null)
                // section is not exists, get the section with the empty name.
                section = iniFile?.GetSection(string.Empty) ?? new IniSection();

            // cycle through the section keys, adding them to the selected custom section.
            foreach (var key in section.Keys)
            {
                // check if the key name has been defined.
                if (!string.IsNullOrWhiteSpace(key.KeyName))
                    SelectedCustomGameUserSetting.Add(key.KeyName, key.KeyValue);
            }
        }

        private void PasteCustomGameUserSettingSections_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => !string.IsNullOrWhiteSpace(s.SectionName) && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                Settings.CustomGameUserSettings.Add(section.SectionName, section.KeysToStringEnumerable(), false);
            }
        }

        private void ReloadCustomGameUserSettings_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ReloadLabel"), _globalizer.GetResourceString("ServerSettings_ReloadTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                // build a full exclusion list
                var exclusions = new List<Enum>();
                foreach (var serverProfileCategory in Enum.GetValues(typeof(ServerProfileCategory)))
                {
                    if ((ServerProfileCategory)serverProfileCategory == ServerProfileCategory.CustomGameUserSettings)
                        continue;

                    exclusions.Add((ServerProfileCategory)serverProfileCategory);
                }

                var configIniFile = Path.Combine(ServerProfile.GetProfileServerConfigDir(Settings), Config.Default.ServerGameUserSettingsConfigFile);
                // load only this section, using the full exclusion list
                var tempServerProfile = ServerProfile.LoadFromConfigFiles(configIniFile, null, exclusions);
                // perform a profile sync
                Settings.SyncSettings(ServerProfileCategory.CustomGameUserSettings, tempServerProfile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ReloadErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveCustomGameUserSettingItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (SelectedCustomGameUserSetting == null)
                return;

            var item = ((CustomItem)((Button)e.Source).DataContext);
            SelectedCustomGameUserSetting.Remove(item);
        }

        private void RemoveCustomGameUserSettingSection_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var section = ((CustomSection)((Button)e.Source).DataContext);
            Settings.CustomGameUserSettings.Remove(section);
        }
        #endregion

        #region Custom GameSettings
        private void AddCustomGameSettingItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedCustomGameSetting?.Add(string.Empty, string.Empty);
        }

        private void AddCustomGameSettingSection_Click(object sender, RoutedEventArgs e)
        {
            Settings.CustomGameSettings.Add(string.Empty, new string[0]);
        }

        private void ClearCustomGameSettingItems_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCustomGameSetting?.Clear();
        }

        private void ClearCustomGameSettingSections_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCustomGameSetting = null;
            Settings.CustomGameSettings.Clear();
        }

        private void ImportCustomGameSettingSections_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.EnsureFileExists = true;
            dialog.Multiselect = false;
            dialog.Title = _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Title");
            dialog.Filters.Add(new CommonFileDialogFilter("Ini Files", "*.ini"));
            dialog.InitialDirectory = Settings.InstallDirectory;
            var result = dialog.ShowDialog(Window.GetWindow(this));
            if (result == CommonFileDialogResult.Ok)
            {
                try
                {
                    // read the selected ini file.
                    var iniFile = IniFileUtils.ReadFromFile(dialog.FileName);

                    // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
                    foreach (var section in iniFile.Sections.Where(s => !string.IsNullOrWhiteSpace(s.SectionName) && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
                    {
                        Settings.CustomGameSettings.Add(section.SectionName, section.KeysToStringEnumerable(), false);
                    }

                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Label"), _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void PasteCustomGameSettingItems_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCustomGameSetting == null)
                return;

            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);
            // get the section with the same name as the currently selected custom section.
            var section = iniFile?.GetSection(SelectedCustomGameSetting.SectionName);
            // check if the section exists.
            if (section == null)
                // section is not exists, get the section with the empty name.
                section = iniFile?.GetSection(string.Empty) ?? new IniSection();

            // cycle through the section keys, adding them to the selected custom section.
            foreach (var key in section.Keys)
            {
                // check if the key name has been defined.
                if (!string.IsNullOrWhiteSpace(key.KeyName))
                    SelectedCustomGameSetting.Add(key.KeyName, key.KeyValue);
            }
        }

        private void PasteCustomGameSettingSections_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => !string.IsNullOrWhiteSpace(s.SectionName) && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                Settings.CustomGameSettings.Add(section.SectionName, section.KeysToStringEnumerable(), false);
            }
        }

        private void ReloadCustomGameSettings_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ReloadLabel"), _globalizer.GetResourceString("ServerSettings_ReloadTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                // build a full exclusion list
                var exclusions = new List<Enum>();
                foreach (var serverProfileCategory in Enum.GetValues(typeof(ServerProfileCategory)))
                {
                    if ((ServerProfileCategory)serverProfileCategory == ServerProfileCategory.CustomGameSettings)
                        continue;

                    exclusions.Add((ServerProfileCategory)serverProfileCategory);
                }

                var configIniFile = Path.Combine(ServerProfile.GetProfileServerConfigDir(Settings), Config.Default.ServerGameUserSettingsConfigFile);
                // load only this section, using the full exclusion list
                var tempServerProfile = ServerProfile.LoadFromConfigFiles(configIniFile, null, exclusions);
                // perform a profile sync
                Settings.SyncSettings(ServerProfileCategory.CustomGameSettings, tempServerProfile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ReloadErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveCustomGameSettingItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (SelectedCustomGameSetting == null)
                return;

            var item = ((CustomItem)((Button)e.Source).DataContext);
            SelectedCustomGameSetting.Remove(item);
        }

        private void RemoveCustomGameSettingSection_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var section = ((CustomSection)((Button)e.Source).DataContext);
            Settings.CustomGameSettings.Remove(section);
        }
        #endregion

        #region Custom EngineSettings
        private void AddCustomEngineSettingItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedCustomEngineSetting?.Add(string.Empty, string.Empty);
        }

        private void AddCustomEngineSettingSection_Click(object sender, RoutedEventArgs e)
        {
            Settings.CustomEngineSettings.Add(string.Empty, new string[0]);
        }

        private void ClearCustomEngineSettingItems_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCustomEngineSetting?.Clear();
        }

        private void ClearCustomEngineSettingSections_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCustomEngineSetting = null;
            Settings.CustomEngineSettings.Clear();
        }

        private void ImportCustomEngineSettingSections_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.EnsureFileExists = true;
            dialog.Multiselect = false;
            dialog.Title = _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Title");
            dialog.Filters.Add(new CommonFileDialogFilter("Ini Files", "*.ini"));
            dialog.InitialDirectory = Settings.InstallDirectory;
            var result = dialog.ShowDialog(Window.GetWindow(this));
            if (result == CommonFileDialogResult.Ok)
            {
                try
                {
                    // read the selected ini file.
                    var iniFile = IniFileUtils.ReadFromFile(dialog.FileName);

                    // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
                    foreach (var section in iniFile.Sections.Where(s => !string.IsNullOrWhiteSpace(s.SectionName) && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
                    {
                        Settings.CustomEngineSettings.Add(section.SectionName, section.KeysToStringEnumerable(), false);
                    }

                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Label"), _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void PasteCustomEngineSettingItems_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCustomEngineSetting == null)
                return;

            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);
            // get the section with the same name as the currently selected custom section.
            var section = iniFile?.GetSection(SelectedCustomEngineSetting.SectionName);
            // check if the section exists.
            if (section == null)
                // section is not exists, get the section with the empty name.
                section = iniFile?.GetSection(string.Empty) ?? new IniSection();

            // cycle through the section keys, adding them to the selected custom section.
            foreach (var key in section.Keys)
            {
                // check if the key name has been defined.
                if (!string.IsNullOrWhiteSpace(key.KeyName))
                    SelectedCustomEngineSetting.Add(key.KeyName, key.KeyValue);
            }
        }

        private void PasteCustomEngineSettingSections_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => !string.IsNullOrWhiteSpace(s.SectionName) && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                Settings.CustomEngineSettings.Add(section.SectionName, section.KeysToStringEnumerable(), false);
            }
        }

        private void ReloadCustomEngineSettings_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ReloadLabel"), _globalizer.GetResourceString("ServerSettings_ReloadTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                // build a full exclusion list
                var exclusions = new List<Enum>();
                foreach (var serverProfileCategory in Enum.GetValues(typeof(ServerProfileCategory)))
                {
                    if ((ServerProfileCategory)serverProfileCategory == ServerProfileCategory.CustomEngineSettings)
                        continue;

                    exclusions.Add((ServerProfileCategory)serverProfileCategory);
                }

                var configIniFile = Path.Combine(ServerProfile.GetProfileServerConfigDir(Settings), Config.Default.ServerGameUserSettingsConfigFile);
                // load only this section, using the full exclusion list
                var tempServerProfile = ServerProfile.LoadFromConfigFiles(configIniFile, null, exclusions);
                // perform a profile sync
                Settings.SyncSettings(ServerProfileCategory.CustomEngineSettings, tempServerProfile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ReloadErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveCustomEngineSettingItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (SelectedCustomEngineSetting == null)
                return;

            var item = ((CustomItem)((Button)e.Source).DataContext);
            SelectedCustomEngineSetting.Remove(item);
        }

        private void RemoveCustomEngineSettingSection_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var section = ((CustomSection)((Button)e.Source).DataContext);
            Settings.CustomEngineSettings.Remove(section);
        }
        #endregion

        #region Custom Levels 
        private CommonFileDialog GetCustomLevelCommonFileDialog(ServerSettingsCustomLevelsAction action)
        {
            CommonFileDialog dialog = null;

            switch (action)
            {
                case ServerSettingsCustomLevelsAction.ExportDinoLevels:
                case ServerSettingsCustomLevelsAction.ExportPlayerLevels:
                    dialog = new CommonSaveFileDialog();
                    dialog.Title = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportDialogTitle");
                    dialog.DefaultExtension = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportDefaultExtension");
                    dialog.Filters.Add(new CommonFileDialogFilter(GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportFilterLabel"), GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportFilterExtension")));
                    break;

                case ServerSettingsCustomLevelsAction.ImportDinoLevels:
                case ServerSettingsCustomLevelsAction.ImportPlayerLevels:
                    dialog = new CommonOpenFileDialog();
                    dialog.Title = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportDialogTitle");
                    dialog.DefaultExtension = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportDefaultExtension");
                    dialog.Filters.Add(new CommonFileDialogFilter(GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportFilterLabel"), GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportFilterExtension")));
                    break;
            }

            return dialog;
        }

        public ICommand CustomLevelActionCommand
        {
            get
            {
                return new RelayCommand<ServerSettingsCustomLevelsAction>(
                    execute: (action) =>
                    {
                        var errorTitle = GlobalizedApplication.Instance.GetResourceString("Generic_ErrorLabel");

                        try
                        {
                            var dialog = GetCustomLevelCommonFileDialog(action);
                            var dialogValue = string.Empty;
                            if (dialog != null && dialog.ShowDialog(Window.GetWindow(this)) == CommonFileDialogResult.Ok)
                                dialogValue = dialog.FileName;

                            switch (action)
                            {
                                case ServerSettingsCustomLevelsAction.ExportDinoLevels:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportErrorTitle");

                                    this.Settings.ExportDinoLevels(dialogValue);
                                    break;

                                case ServerSettingsCustomLevelsAction.ImportDinoLevels:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportErrorTitle");

                                    this.Settings.ImportDinoLevels(dialogValue);
                                    break;

                                case ServerSettingsCustomLevelsAction.UpdateDinoXPCap:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_UpdateErrorTitle");

                                    this.Settings.UpdateOverrideMaxExperiencePointsDino();
                                    break;

                                case ServerSettingsCustomLevelsAction.ExportPlayerLevels:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportErrorTitle");

                                    this.Settings.ExportPlayerLevels(dialogValue);
                                    break;

                                case ServerSettingsCustomLevelsAction.ImportPlayerLevels:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportErrorTitle");

                                    this.Settings.ImportPlayerLevels(dialogValue);
                                    break;

                                case ServerSettingsCustomLevelsAction.UpdatePlayerXPCap:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_UpdateErrorTitle");

                                    this.Settings.UpdateOverrideMaxExperiencePointsPlayer();
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    },
                    canExecute: (action) => true
                );
            }
        }

        private void AddDinoLevel_Click(object sender, RoutedEventArgs e)
        {
            var level = ((Level)((Button)e.Source).DataContext);
            this.Settings.DinoLevels.AddNewLevel(level, Config.Default.CustomLevelXPIncrease_Dino);
        }

        private void AddPlayerLevel_Click(object sender, RoutedEventArgs e)
        {
            var level = ((Level)((Button)e.Source).DataContext);
            this.Settings.PlayerLevels.AddNewLevel(level, Config.Default.CustomLevelXPIncrease_Player);
        }

        private void DinoLevels_Clear(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoLevels_ClearLabel"), _globalizer.GetResourceString("ServerSettings_DinoLevels_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ClearLevelProgression(LevelProgression.Dino);
        }

        private void DinoLevels_Recalculate(object sender, RoutedEventArgs e)
        {
            this.Settings.DinoLevels.UpdateTotals();
            this.CustomDinoLevelsView.Items.Refresh();
        }

        private void DinoLevels_ResetOfficial(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoLevels_ResetLabel"), _globalizer.GetResourceString("ServerSettings_DinoLevels_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetLevelProgressionToOfficial(LevelProgression.Dino);
        }

        private void MaxXPPlayer_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PlayerMaxXP_ResetLabel"), _globalizer.GetResourceString("ServerSettings_PlayerMaxXP_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetOverrideMaxExperiencePointsPlayer();
        }

        private void MaxXPDino_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoMaxXP_ResetLabel"), _globalizer.GetResourceString("ServerSettings_DinoMaxXP_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetOverrideMaxExperiencePointsDino();
        }

        private void PlayerLevels_Clear(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PlayerLevels_ClearLabel"), _globalizer.GetResourceString("ServerSettings_PlayerLevels_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ClearLevelProgression(LevelProgression.Player);
        }

        private void PlayerLevels_Recalculate(object sender, RoutedEventArgs e)
        {
            this.Settings.PlayerLevels.UpdateTotals();
            this.CustomPlayerLevelsView.Items.Refresh();
        }

        private void PlayerLevels_ResetOfficial(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PlayerLevels_ResetLabel"), _globalizer.GetResourceString("ServerSettings_PlayerLevels_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetLevelProgressionToOfficial(LevelProgression.Player);
        }

        private void RemoveDinoLevel_Click(object sender, RoutedEventArgs e)
        {
            if (this.Settings.DinoLevels.Count == 1)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_CustomLevels_LastRemove_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_CustomLevels_LastRemove_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                var level = ((Level)((Button)e.Source).DataContext);
                this.Settings.DinoLevels.RemoveLevel(level);
            }
        }

        private void RemovePlayerLevel_Click(object sender, RoutedEventArgs e)
        {
            if (this.Settings.PlayerLevels.Count == 1)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_CustomLevels_LastRemove_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_CustomLevels_LastRemove_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                var level = ((Level)((Button)e.Source).DataContext);
                this.Settings.PlayerLevels.RemoveLevel(level);
            }
        }

        private void EnableLevelProgressions_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            if (!Settings.EnableLevelProgressions)
            {
                Settings.EnableDinoLevelProgressions = false;
            }
        }
        #endregion

        #region Server Files 
        private void AddAdminPlayer_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddUserWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    Settings.DestroyServerFilesWatcher();

                    var steamIdsString = window.Users;
                    var steamIds = steamIdsString.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var steamUsers = SteamUtils.GetSteamUserDetails(steamIds.ToList());
                    var steamUserList = PlayerUserList.GetList(steamUsers, steamIds);
                    Settings.ServerFilesAdmins.AddRange(steamUserList);

                    Settings.SaveServerFileAdministrators();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Settings.SetupServerFilesWatcher();
                }
            }
        }

        private void AddExclusivePlayer_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddUserWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    Settings.DestroyServerFilesWatcher();

                    var steamIdsString = window.Users;
                    var steamIds = steamIdsString.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var steamUsers = SteamUtils.GetSteamUserDetails(steamIds.ToList());
                    var steamUserList = PlayerUserList.GetList(steamUsers, steamIds);
                    Settings.ServerFilesExclusive.AddRange(steamUserList);

                    Settings.SaveServerFileExclusive();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Settings.SetupServerFilesWatcher();
                }
            }
        }

        private void AddWhitelistPlayer_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddUserWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    Settings.DestroyServerFilesWatcher();

                    var steamIdsString = window.Users;
                    var steamIds = steamIdsString.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var steamUsers = SteamUtils.GetSteamUserDetails(steamIds.ToList());
                    var steamUserList = PlayerUserList.GetList(steamUsers, steamIds);
                    Settings.ServerFilesWhitelisted.AddRange(steamUserList);

                    Settings.SaveServerFileWhitelisted();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Settings.SetupServerFilesWatcher();
                }
            }
        }

        private void ClearAdminPlayers_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                Settings.DestroyServerFilesWatcher();

                Settings.ServerFilesAdmins.Clear();
                Settings.SaveServerFileAdministrators();
            }
            finally
            {
                Settings.SetupServerFilesWatcher();
            }
        }

        private void ClearExclusivePlayers_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                Settings.DestroyServerFilesWatcher();

                Settings.ServerFilesExclusive.Clear();
                Settings.SaveServerFileExclusive();
            }
            finally
            {
                Settings.SetupServerFilesWatcher();
            }
        }

        private void ClearWhitelistPlayers_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                Settings.DestroyServerFilesWatcher();

                Settings.ServerFilesWhitelisted.Clear();
                Settings.SaveServerFileWhitelisted();
            }
            finally
            {
                Settings.SetupServerFilesWatcher();
            }
        }

        private async void ReloadAdminPlayers_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                Settings.LoadServerFiles(true, false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void ReloadExclusivePlayers_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                Settings.LoadServerFiles(false, true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void ReloadWhitelistPlayers_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                Settings.LoadServerFiles(false, false, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void RemoveAdminPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                Settings.DestroyServerFilesWatcher();

                var mod = ((PlayerUserItem)((Button)e.Source).DataContext);
                Settings.ServerFilesAdmins.Remove(mod.PlayerId);

                Settings.SaveServerFileAdministrators();
            }
            finally
            {
                Settings.SetupServerFilesWatcher();
            }
        }

        private void RemoveExclusivePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                Settings.DestroyServerFilesWatcher();

                var mod = ((PlayerUserItem)((Button)e.Source).DataContext);
                Settings.ServerFilesExclusive.Remove(mod.PlayerId);

                Settings.SaveServerFileExclusive();
            }
            finally
            {
                Settings.SetupServerFilesWatcher();
            }
        }

        private void RemoveWhitelistPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                Settings.DestroyServerFilesWatcher();

                var mod = ((PlayerUserItem)((Button)e.Source).DataContext);
                Settings.ServerFilesWhitelisted.Remove(mod.PlayerId);

                Settings.SaveServerFileWhitelisted();
            }
            finally
            {
                Settings.SetupServerFilesWatcher();
            }
        }
        #endregion

        #region PGM Settings
        private void PastePGMSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.ConfigDataTextWrapping = TextWrapping.Wrap;
            window.Title = _globalizer.GetResourceString("ServerSettings_PGM_PasteSettingsTitle");
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PGM_PasteSettingsConfirmLabel"), _globalizer.GetResourceString("ServerSettings_PGM_PasteSettingsConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            var prop = Settings.GetType().GetProperty(nameof(Settings.PGM_Terrain));
            if (prop == null)
                return;
            var attr = prop.GetCustomAttributes(typeof(IniFileEntryAttribute), false).OfType<IniFileEntryAttribute>().FirstOrDefault();
            var keyName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                foreach (var key in section.Keys.Where(s => s.KeyName.Equals(keyName)))
                {
                    Settings.PGM_Terrain.InitializeFromINIValue(key.KeyValue);
                }
            }
        }

        private void SavePGMSettings_Click(object sender, RoutedEventArgs e)
        {
            var prop = Settings.GetType().GetProperty(nameof(Settings.PGM_Terrain));
            if (prop == null)
                return;
            var attr = prop.GetCustomAttributes(typeof(IniFileEntryAttribute), false).OfType<IniFileEntryAttribute>().FirstOrDefault();
            var iniName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;
            var iniValue = $"{iniName}={Settings.PGM_Terrain.ToINIValue()}";

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_PGM_SaveSettingsTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void RandomPGMSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.RandomizePGMSettings();
        }
        #endregion

        #region Map Spawner Overrides
        private void AddNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            Settings.NPCSpawnSettings.Add(new NPCSpawnSettings());
            Settings.NPCSpawnSettings.Update();
        }

        private void AddNPCSpawnEntry_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedNPCSpawnSetting == null)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_AddChildErrorLabel"), _globalizer.GetResourceString("ServerSettings_AddChildErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SelectedNPCSpawnSetting.NPCSpawnEntrySettings.Add(new NPCSpawnEntrySettings());
            Settings.NPCSpawnSettings.Update();
        }

        private void ClearNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedNPCSpawnSetting = null;
            Settings.NPCSpawnSettings.Clear();
            Settings.NPCSpawnSettings.Update();
        }

        private void ClearNPCSpawnEntry_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedNPCSpawnSetting?.NPCSpawnEntrySettings.Clear();
            Settings.NPCSpawnSettings.Update();
        }

        private void PasteNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData.Replace(" ", ""));

            Server.Profile.NPCSpawnSettings.RenderToModel();

            // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                var configAddNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(Server.Profile.ConfigAddNPCSpawnEntriesContainer), NPCSpawnContainerType.Add);
                configAddNPCSpawnEntriesContainer.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{configAddNPCSpawnEntriesContainer.IniCollectionKey}=")));
                Server.Profile.ConfigAddNPCSpawnEntriesContainer.AddRange(configAddNPCSpawnEntriesContainer);
                Server.Profile.ConfigAddNPCSpawnEntriesContainer.IsEnabled |= configAddNPCSpawnEntriesContainer.IsEnabled;

                var configSubtractNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(Server.Profile.ConfigSubtractNPCSpawnEntriesContainer), NPCSpawnContainerType.Subtract);
                configSubtractNPCSpawnEntriesContainer.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{configSubtractNPCSpawnEntriesContainer.IniCollectionKey}=")));
                Server.Profile.ConfigSubtractNPCSpawnEntriesContainer.AddRange(configSubtractNPCSpawnEntriesContainer);
                Server.Profile.ConfigSubtractNPCSpawnEntriesContainer.IsEnabled |= configSubtractNPCSpawnEntriesContainer.IsEnabled;

                var configOverrideNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(Server.Profile.ConfigOverrideNPCSpawnEntriesContainer), NPCSpawnContainerType.Override);
                configOverrideNPCSpawnEntriesContainer.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{configOverrideNPCSpawnEntriesContainer.IniCollectionKey}=")));
                Server.Profile.ConfigOverrideNPCSpawnEntriesContainer.AddRange(configOverrideNPCSpawnEntriesContainer);
                Server.Profile.ConfigOverrideNPCSpawnEntriesContainer.IsEnabled |= configOverrideNPCSpawnEntriesContainer.IsEnabled;
            }

            Server.Profile.NPCSpawnSettings = new NPCSpawnSettingsList(Server.Profile.ConfigAddNPCSpawnEntriesContainer, Server.Profile.ConfigSubtractNPCSpawnEntriesContainer, Server.Profile.ConfigOverrideNPCSpawnEntriesContainer);
            Server.Profile.NPCSpawnSettings.RenderToView();

            RefreshBaseMapSpawnerList();
            RefreshBaseDinoList();
        }

        private void RemoveNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((NPCSpawnSettings)((Button)e.Source).DataContext);
            Settings.NPCSpawnSettings.Remove(item);
            Settings.NPCSpawnSettings.Update();
        }

        private void RemoveNPCSpawnEntry_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedNPCSpawnSetting == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((NPCSpawnEntrySettings)((Button)e.Source).DataContext);
            SelectedNPCSpawnSetting.NPCSpawnEntrySettings.Remove(item);
            Settings.NPCSpawnSettings.Update();
        }

        private void SaveNPCSpawns_Click(object sender, RoutedEventArgs e)
        {
            Settings.NPCSpawnSettings.RenderToModel();

            var iniValues = new List<string>();
            iniValues.AddRange(Settings.ConfigAddNPCSpawnEntriesContainer.ToIniValues());
            iniValues.AddRange(Settings.ConfigSubtractNPCSpawnEntriesContainer.ToIniValues());
            iniValues.AddRange(Settings.ConfigOverrideNPCSpawnEntriesContainer.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_MapSpawnerOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void SaveNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            var item = ((NPCSpawnSettings)((Button)e.Source).DataContext);
            if (item == null)
                return;

            Settings.NPCSpawnSettings.RenderToModel();

            string iniName = null;
            string iniValue = null;
            switch (item.ContainerType)
            {
                case NPCSpawnContainerType.Add:
                    iniName = Settings.ConfigAddNPCSpawnEntriesContainer.IniCollectionKey;
                    var addItem = Settings.ConfigAddNPCSpawnEntriesContainer.FirstOrDefault(i => i.UniqueId == item.UniqueId);
                    iniValue = $"{iniName}={addItem?.ToIniValue(Settings.ConfigAddNPCSpawnEntriesContainer.ContainerType)}";
                    break;
                case NPCSpawnContainerType.Subtract:
                    iniName = Settings.ConfigSubtractNPCSpawnEntriesContainer.IniCollectionKey;
                    var subtractItem = Settings.ConfigSubtractNPCSpawnEntriesContainer.FirstOrDefault(i => i.UniqueId == item.UniqueId);
                    iniValue = $"{iniName}={subtractItem?.ToIniValue(Settings.ConfigSubtractNPCSpawnEntriesContainer.ContainerType)}";
                    break;
                case NPCSpawnContainerType.Override:
                    iniName = Settings.ConfigOverrideNPCSpawnEntriesContainer.IniCollectionKey;
                    var overrideItem = Settings.ConfigOverrideNPCSpawnEntriesContainer.FirstOrDefault(i => i.UniqueId == item.UniqueId);
                    iniValue = $"{iniName}={overrideItem?.ToIniValue(Settings.ConfigOverrideNPCSpawnEntriesContainer.ContainerType)}";
                    break;
                default:
                    return;
            }

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_MapSpawnerOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Supply Crate Overrides
        private void AddSupplyCrate_Click(object sender, RoutedEventArgs e)
        {
            Settings.ConfigOverrideSupplyCrateItems.Add(new SupplyCrateOverride());
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void AddSupplyCrateItemSet_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplyCrateOverride == null)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_AddChildErrorLabel"), _globalizer.GetResourceString("ServerSettings_AddChildErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SelectedSupplyCrateOverride.ItemSets.Add(new SupplyCrateItemSet());
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void AddSupplyCrateItemSetEntry_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplyCrateItemSet == null)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_AddChildErrorLabel"), _globalizer.GetResourceString("ServerSettings_AddChildErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SelectedSupplyCrateItemSet.ItemEntries.Add(new SupplyCrateItemSetEntry());
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void AddSupplyCrateItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplyCrateItemSetEntry == null)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_AddChildErrorLabel"), _globalizer.GetResourceString("ServerSettings_AddChildErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SelectedSupplyCrateItemSetEntry.Items.Add(new SupplyCrateItemEntrySettings());
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void ClearSupplyCrates_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedSupplyCrateItemSetEntry = null;
            SelectedSupplyCrateItemSet = null;
            SelectedSupplyCrateOverride = null;
            Settings.ConfigOverrideSupplyCrateItems.Clear();
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void ClearSupplyCrateItemSets_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedSupplyCrateItemSetEntry = null;
            SelectedSupplyCrateItemSet = null;
            SelectedSupplyCrateOverride?.ItemSets.Clear();
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void ClearSupplyCrateItemSetEntries_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedSupplyCrateItemSetEntry = null;
            SelectedSupplyCrateItemSet?.ItemEntries.Clear();
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void ClearSupplyCrateItems_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedSupplyCrateItemSetEntry?.Items.Clear();
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void PasteSupplyCrate_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData.Replace(" ", ""));

            Server.Profile.ConfigOverrideSupplyCrateItems.RenderToModel();

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                var configOverrideSupplyCrateItems = new SupplyCrateOverrideList(nameof(Server.Profile.ConfigOverrideSupplyCrateItems));
                configOverrideSupplyCrateItems.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{configOverrideSupplyCrateItems.IniCollectionKey}=")));
                Server.Profile.ConfigOverrideSupplyCrateItems.AddRange(configOverrideSupplyCrateItems);
                Server.Profile.ConfigOverrideSupplyCrateItems.IsEnabled |= configOverrideSupplyCrateItems.IsEnabled;
            }

            var errors = Server.Profile.ConfigOverrideSupplyCrateItems.RenderToView();

            RefreshBaseSupplyCrateList();
            RefreshBasePrimalItemList();

            if (errors.Any())
            {
                var error = $"The following errors have been found:\r\n\r\n{string.Join("\r\n", errors)}";

                var window2 = new CommandLineWindow(error);
                window2.OutputTextWrapping = TextWrapping.NoWrap;
                window2.Height = 500;
                window2.Title = "Import Errors";
                window2.Owner = Window.GetWindow(this);
                window2.ShowDialog();
            }
        }

        private void RemoveSupplyCrate_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((SupplyCrateOverride)((Button)e.Source).DataContext);
            Settings.ConfigOverrideSupplyCrateItems.Remove(item);
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void RemoveSupplyCrateItemSet_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplyCrateOverride == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((SupplyCrateItemSet)((Button)e.Source).DataContext);
            SelectedSupplyCrateOverride.ItemSets.Remove(item);
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void RemoveSupplyCrateItemSetEntry_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplyCrateItemSet == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((SupplyCrateItemSetEntry)((Button)e.Source).DataContext);
            SelectedSupplyCrateItemSet.ItemEntries.Remove(item);
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void RemoveSupplyCrateItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplyCrateItemSetEntry == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((SupplyCrateItemEntrySettings)((Button)e.Source).DataContext);
            SelectedSupplyCrateItemSetEntry.Items.Remove(item);
            Settings.ConfigOverrideSupplyCrateItems.Update();
        }

        private void SaveSupplyCrates_Click(object sender, RoutedEventArgs e)
        {
            Settings.ConfigOverrideSupplyCrateItems.RenderToModel();

            var iniValues = new List<string>();
            iniValues.AddRange(Settings.ConfigOverrideSupplyCrateItems.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_SupplyCrate_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void SaveSupplyCrate_Click(object sender, RoutedEventArgs e)
        {
            var item = ((SupplyCrateOverride)((Button)e.Source).DataContext);
            if (item == null)
                return;

            Settings.ConfigOverrideSupplyCrateItems.RenderToModel();

            var iniName = Settings.ConfigOverrideSupplyCrateItems.IniCollectionKey;
            var iniValue = $"{iniName}={item.ToINIValue()}";

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_SupplyCrate_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Exclude Item Indices Overrides
        private void AddExcludeItemIndicesOverride_Click(object sender, RoutedEventArgs e)
        {
            Settings.ExcludeItemIndices.Add(new ExcludeItemIndicesOverride());
            Settings.ExcludeItemIndices.Update();
        }

        private void ClearExcludeItemIndicesOverrides_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Settings.ExcludeItemIndices.Clear();
            Settings.ExcludeItemIndices.Update();
        }

        private void PasteExcludeItemIndicesOverride_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData.Replace(" ", ""));

            Server.Profile.ExcludeItemIndices.RenderToModel();

            // cycle through the sections, adding them to the list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                var excludeItemIndices = new AggregateIniValueList<ExcludeItemIndicesOverride>(nameof(Server.Profile.ExcludeItemIndices), null);
                excludeItemIndices.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{excludeItemIndices.IniCollectionKey}=")));
                Server.Profile.ExcludeItemIndices.AddRange(excludeItemIndices);
                Server.Profile.ExcludeItemIndices.IsEnabled |= excludeItemIndices.IsEnabled;
            }

            var errors = Server.Profile.ExcludeItemIndices.RenderToView();

            RefreshBaseDinoList();

            if (errors.Any())
            {
                var error = $"The following errors have been found:\r\n\r\n{string.Join("\r\n", errors)}";

                var window2 = new CommandLineWindow(error);
                window2.OutputTextWrapping = TextWrapping.NoWrap;
                window2.Height = 500;
                window2.Title = "Import Errors";
                window2.Owner = Window.GetWindow(this);
                window2.ShowDialog();
            }
        }

        private void RemoveExcludeItemIndicesOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((ExcludeItemIndicesOverride)((Button)e.Source).DataContext);
            Settings.ExcludeItemIndices.Remove(item);
            Settings.ExcludeItemIndices.Update();
        }

        private void SaveExcludeItemIndicesOverride_Click(object sender, RoutedEventArgs e)
        {
            Settings.ExcludeItemIndices.RenderToModel();

            var iniValues = new List<string>();
            iniValues.AddRange(Settings.ExcludeItemIndices.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_ExcludeItemIndicesOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void SaveExcludeItemIndicesOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            var item = ((ExcludeItemIndicesOverride)((Button)e.Source).DataContext);
            if (item == null)
                return;

            Settings.ExcludeItemIndices.RenderToModel();

            var iniName = Settings.ExcludeItemIndices.IniCollectionKey;
            var iniValue = $"{iniName}={item.ToINIValue()}";

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_ExcludeItemIndicesOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Stack Size Overrides
        private void AddStackSizeOverride_Click(object sender, RoutedEventArgs e)
        {
            Settings.ConfigOverrideItemMaxQuantity.Add(new StackSizeOverride());
            Settings.ConfigOverrideItemMaxQuantity.Update();
        }

        private void ClearStackSizeOverrides_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Settings.ConfigOverrideItemMaxQuantity.Clear();
            Settings.ConfigOverrideItemMaxQuantity.Update();
        }

        private void PasteStackSizeOverride_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData.Replace(" ", ""));

            Server.Profile.ConfigOverrideItemMaxQuantity.RenderToModel();

            // cycle through the sections, adding them to the list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                var configOverrideItemMaxQuantity = new AggregateIniValueList<StackSizeOverride>(nameof(Server.Profile.ConfigOverrideItemMaxQuantity), null);
                configOverrideItemMaxQuantity.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{configOverrideItemMaxQuantity.IniCollectionKey}=")));
                Server.Profile.ConfigOverrideItemMaxQuantity.AddRange(configOverrideItemMaxQuantity);
                Server.Profile.ConfigOverrideItemMaxQuantity.IsEnabled |= configOverrideItemMaxQuantity.IsEnabled;
            }

            var errors = Server.Profile.ConfigOverrideItemMaxQuantity.RenderToView();

            RefreshBasePrimalItemList();

            if (errors.Any())
            {
                var error = $"The following errors have been found:\r\n\r\n{string.Join("\r\n", errors)}";

                var window2 = new CommandLineWindow(error);
                window2.OutputTextWrapping = TextWrapping.NoWrap;
                window2.Height = 500;
                window2.Title = "Import Errors";
                window2.Owner = Window.GetWindow(this);
                window2.ShowDialog();
            }
        }

        private void RemoveStackSizeOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((StackSizeOverride)((Button)e.Source).DataContext);
            Settings.ConfigOverrideItemMaxQuantity.Remove(item);
            Settings.ConfigOverrideItemMaxQuantity.Update();
        }

        private void SaveStackSizeOverride_Click(object sender, RoutedEventArgs e)
        {
            Settings.ConfigOverrideItemMaxQuantity.RenderToModel();

            var iniValues = new List<string>();
            iniValues.AddRange(Settings.ConfigOverrideItemMaxQuantity.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_StackSizeOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void SaveStackSizeOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            var item = ((StackSizeOverride)((Button)e.Source).DataContext);
            if (item == null)
                return;

            Settings.ConfigOverrideItemMaxQuantity.RenderToModel();

            var iniName = Settings.ConfigOverrideItemMaxQuantity.IniCollectionKey;
            var iniValue = $"{iniName}={item.ToINIValue()}";

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_StackSizeOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Prevent Transfer Overrides
        private void AddPreventTransferOverride_Click(object sender, RoutedEventArgs e)
        {
            Settings.PreventTransferForClassNames.Add(new PreventTransferOverride());
            Settings.PreventTransferForClassNames.Update();
        }

        private void ClearPreventTransferOverrides_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Settings.PreventTransferForClassNames.Clear();
            Settings.PreventTransferForClassNames.Update();
        }

        private void PastePreventTransferOverride_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData.Replace(" ", ""));

            Server.Profile.PreventTransferForClassNames.RenderToModel();

            // cycle through the sections, adding them to the list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.IniSectionNames.ContainsValue(s.SectionName)))
            {
                var preventTransferForClassNames = new AggregateIniValueList<PreventTransferOverride>(nameof(Server.Profile.PreventTransferForClassNames), null);
                preventTransferForClassNames.FromIniValues(section.KeysToStringEnumerable().Where(s => s.StartsWith($"{preventTransferForClassNames.IniCollectionKey}=")));
                Server.Profile.PreventTransferForClassNames.AddRange(preventTransferForClassNames);
                Server.Profile.PreventTransferForClassNames.IsEnabled |= preventTransferForClassNames.IsEnabled;
            }

            var errors = Server.Profile.PreventTransferForClassNames.RenderToView();

            RefreshBaseDinoList();

            if (errors.Any())
            {
                var error = $"The following errors have been found:\r\n\r\n{string.Join("\r\n", errors)}";

                var window2 = new CommandLineWindow(error);
                window2.OutputTextWrapping = TextWrapping.NoWrap;
                window2.Height = 500;
                window2.Title = "Import Errors";
                window2.Owner = Window.GetWindow(this);
                window2.ShowDialog();
            }
        }

        private void RemovePreventTransferOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((PreventTransferOverride)((Button)e.Source).DataContext);
            Settings.PreventTransferForClassNames.Remove(item);
            Settings.PreventTransferForClassNames.Update();
        }

        private void SavePreventTransferOverride_Click(object sender, RoutedEventArgs e)
        {
            Settings.PreventTransferForClassNames.RenderToModel();

            var iniValues = new List<string>();
            iniValues.AddRange(Settings.PreventTransferForClassNames.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_PreventTransferOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void SavePreventTransferOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            var item = ((PreventTransferOverride)((Button)e.Source).DataContext);
            if (item == null)
                return;

            Settings.PreventTransferForClassNames.RenderToModel();

            var iniName = Settings.PreventTransferForClassNames.IniCollectionKey;
            var iniValue = $"{iniName}={item.ToINIValue()}";

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_PreventTransferOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #endregion

        #region Methods
        public void CloseControl()
        {
            GameData.GameDataLoaded -= GameData_GameDataLoaded;
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent -= ResourceDictionaryChangedEvent;
        }

        public void RefreshCultureList()
        {
            var newList = new ComboBoxItemList();

            string[] culture = { "ca", "cs", "da", "de", "en", "es", "eu", "fi", "fr", "hu", "it", "ja", "ka", "ko", "nl", "pl", "pt_BR", "ru", "sv", "th", "tr", "zh", "zh-Hans-CN", "zh-TW" };
            foreach (var lang in culture)
            {
                newList.Add(new Common.Model.ComboBoxItem
                {
                    DisplayMember = lang,
                    ValueMember = lang,
                });
            }

            this.Culture = newList;
            this.CultureComboBox.SelectedValue = this.Settings.Culture;
        }

        public void RefreshBaseDinoModList()
        {
            var selectedValue = SelectedModDino;
            var newList = new ComboBoxItemList();

            var value = GameData.MOD_ALL;
            var name = _globalizer.GetResourceString($"Mod_{value}");
            newList.Add(new Common.Model.ComboBoxItem(value, name));

            var values = GameData.GetDinoSpawns().GroupBy(d => d.Mod).OrderBy(g => g.Key).Select(g => g.Key);
            foreach (var modValue in values)
            {
                if (string.IsNullOrWhiteSpace(modValue))
                    continue;

                var modName = _globalizer.GetResourceString($"Mod_{modValue}");
                if (string.IsNullOrWhiteSpace(modName))
                    modName = modValue;

                newList.Add(new Common.Model.ComboBoxItem(modValue, modName));
            }

            value = GameData.MOD_UNKNOWN;
            name = _globalizer.GetResourceString($"Mod_{value}");
            newList.Add(new Common.Model.ComboBoxItem(value, name));

            this.BaseDinoModList = newList;
            this.ModDinoComboBox.SelectedValue = selectedValue;
        }

        public void RefreshBaseEngramModList()
        {
            var selectedValue = SelectedModEngram;
            var newList = new ComboBoxItemList();

            var value = GameData.MOD_ALL;
            var name = _globalizer.GetResourceString($"Mod_{value}");
            newList.Add(new Common.Model.ComboBoxItem(value, name));

            var values = GameData.GetEngrams().GroupBy(d => d.Mod).OrderBy(g => g.Key).Select(g => g.Key);
            foreach (var modValue in values)
            {
                if (string.IsNullOrWhiteSpace(modValue))
                    continue;

                var modName = _globalizer.GetResourceString($"Mod_{modValue}");
                if (string.IsNullOrWhiteSpace(modName))
                    modName = modValue;

                newList.Add(new Common.Model.ComboBoxItem(modValue, modName));
            }

            value = GameData.MOD_UNKNOWN;
            name = _globalizer.GetResourceString($"Mod_{value}");
            newList.Add(new Common.Model.ComboBoxItem(value, name));

            this.BaseEngramModList = newList;
            this.ModEngramComboBox.SelectedValue = selectedValue;
        }

        public void RefreshBaseResourceModList()
        {
            var selectedValue = SelectedModResource;
            var newList = new ComboBoxItemList();

            var value = GameData.MOD_ALL;
            var name = _globalizer.GetResourceString($"Mod_{value}");
            newList.Add(new Common.Model.ComboBoxItem(value, name));

            var values = GameData.GetResourceMultipliers().GroupBy(d => d.Mod).OrderBy(g => g.Key).Select(g => g.Key);
            foreach (var modValue in values)
            {
                if (string.IsNullOrWhiteSpace(modValue))
                    continue;

                var modName = _globalizer.GetResourceString($"Mod_{modValue}");
                if (string.IsNullOrWhiteSpace(modName))
                    modName = modValue;

                newList.Add(new Common.Model.ComboBoxItem(modValue, modName));
            }

            value = GameData.MOD_UNKNOWN;
            name = _globalizer.GetResourceString($"Mod_{value}");
            newList.Add(new Common.Model.ComboBoxItem(value, name));

            this.BaseResourceModList = newList;
            this.ModResourceComboBox.SelectedValue = selectedValue;
        }

        public void RefreshBaseDinoList()
        {
            var newList = new ComboBoxItemList();

            foreach (var dino in GameData.GetDinoSpawns())
            {
                if (string.IsNullOrWhiteSpace(dino.ClassName))
                    continue;

                newList.Add(new Common.Model.ComboBoxItem
                {
                    DisplayMember = string.IsNullOrWhiteSpace(dino.Mod) ? $"{dino.DisplayName}" : $"({dino.DisplayMod}) {dino.DisplayName}",
                    ValueMember = dino.ClassName,
                });
            }

            newList.Sort(i => $"{i.GroupMember}||{i.DisplayMember}");

            foreach (var dinoSetting in this.Settings.DinoSettings)
            {
                if (string.IsNullOrWhiteSpace(dinoSetting.ReplacementClass))
                    continue;

                if (!newList.Any(s => s.ValueMember.Equals(dinoSetting.ReplacementClass)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = dinoSetting.ReplacementClass,
                        ValueMember = dinoSetting.ReplacementClass,
                    });
                }
            }

            foreach (var spawnSetting in this.Settings.NPCSpawnSettings)
            {
                foreach (var spawnEntry in spawnSetting.NPCSpawnEntrySettings)
                {
                    if (string.IsNullOrWhiteSpace(spawnEntry.NPCClassString))
                        continue;

                    if (!newList.Any(s => s.ValueMember.Equals(spawnEntry.NPCClassString)))
                    {
                        newList.Add(new Common.Model.ComboBoxItem
                        {
                            DisplayMember = spawnEntry.NPCClassString,
                            ValueMember = spawnEntry.NPCClassString,
                        });
                    }
                }
            }

            foreach (var preventTransfer in this.Settings.PreventTransferForClassNames)
            {
                if (string.IsNullOrWhiteSpace(preventTransfer.DinoClassString))
                    continue;

                if (!newList.Any(s => s.ValueMember.Equals(preventTransfer.DinoClassString)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = preventTransfer.DinoClassString,
                        ValueMember = preventTransfer.DinoClassString,
                    });
                }
            }

            try
            {
                this.DinoSettingsGrid.BeginInit();
                this.NPCSpawnEntrySettingsGrid.BeginInit();
                this.PreventTransferOverrideGrid.BeginInit();

                this.BaseDinoList = newList;
            }
            finally
            {
                this.DinoSettingsGrid.EndInit();
                this.NPCSpawnEntrySettingsGrid.EndInit();
                this.PreventTransferOverrideGrid.EndInit();
            }
        }

        public void RefreshBaseMapSpawnerList()
        {
            var newList = new ComboBoxItemList();

            foreach (var mapSpawner in GameData.GetMapSpawners())
            {
                newList.Add(new Common.Model.ComboBoxItem
                {
                    DisplayMember = string.IsNullOrWhiteSpace(mapSpawner.Mod) ? $"{mapSpawner.DisplayName}" : $"({mapSpawner.DisplayMod}) {mapSpawner.DisplayName}",
                    ValueMember = mapSpawner.ClassName,
                });
            }

            newList.Sort(i => $"{i.GroupMember}||{i.DisplayMember}");

            foreach (var spawnSetting in this.Settings.NPCSpawnSettings)
            {
                if (string.IsNullOrWhiteSpace(spawnSetting.NPCSpawnEntriesContainerClassString))
                    continue;

                if (!newList.Any(s => s.ValueMember.Equals(spawnSetting.NPCSpawnEntriesContainerClassString)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = spawnSetting.NPCSpawnEntriesContainerClassString,
                        ValueMember = spawnSetting.NPCSpawnEntriesContainerClassString,
                    });
                }
            }

            try
            {
                this.NPCSpawnSettingsGrid.BeginInit();

                this.BaseMapSpawnerList = newList;
            }
            finally
            {
                this.NPCSpawnSettingsGrid.EndInit();
            }
        }

        public void RefreshBasePrimalItemList()
        {
            var newList = new ComboBoxItemList();

            foreach (var primalItem in GameData.GetItems())
            {
                newList.Add(new Common.Model.ComboBoxItem
                {
                    DisplayMember = string.IsNullOrWhiteSpace(primalItem.Mod) ? $"{primalItem.DisplayName}" : $"({primalItem.DisplayMod}) {primalItem.DisplayName}",
                    ValueMember = primalItem.ClassName,
                });
            }

            newList.Sort(i => $"{i.GroupMember}||{i.DisplayMember}");

            foreach (var craftingItem in this.Settings.ConfigOverrideItemCraftingCosts)
            {
                if (string.IsNullOrWhiteSpace(craftingItem.ItemClassString))
                    continue;

                if (!newList.Any(s => s.ValueMember.Equals(craftingItem.ItemClassString)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = craftingItem.ItemClassString,
                        ValueMember = craftingItem.ItemClassString,
                    });
                }

                foreach (var craftingResource in craftingItem.BaseCraftingResourceRequirements)
                {
                    if (string.IsNullOrWhiteSpace(craftingResource.ResourceItemTypeString))
                        continue;

                    if (!newList.Any(s => s.ValueMember.Equals(craftingResource.ResourceItemTypeString)))
                    {
                        newList.Add(new Common.Model.ComboBoxItem
                        {
                            DisplayMember = craftingResource.ResourceItemTypeString,
                            ValueMember = craftingResource.ResourceItemTypeString,
                        });
                    }
                }
            }

            foreach (var supplyCrate in this.Settings.ConfigOverrideSupplyCrateItems)
            {
                foreach (var itemSet in supplyCrate.ItemSets)
                {
                    foreach (var itemEntry in itemSet.ItemEntries)
                    {
                        foreach (var itemClass in itemEntry.Items)
                        {
                            if (string.IsNullOrWhiteSpace(itemClass.ItemClassString))
                                continue;

                            if (!newList.Any(s => s.ValueMember.Equals(itemClass.ItemClassString)))
                            {
                                newList.Add(new Common.Model.ComboBoxItem
                                {
                                    DisplayMember = itemClass.ItemClassString,
                                    ValueMember = itemClass.ItemClassString,
                                });
                            }
                        }
                    }
                }
            }

            foreach (var stackSize in this.Settings.ConfigOverrideItemMaxQuantity)
            {
                if (string.IsNullOrWhiteSpace(stackSize.ItemClassString))
                    continue;

                if (!newList.Any(s => s.ValueMember.Equals(stackSize.ItemClassString)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = stackSize.ItemClassString,
                        ValueMember = stackSize.ItemClassString,
                    });
                }
            }

            try
            {
                this.CraftingOverrideItemGrid.BeginInit();
                this.CraftingOverrideResourceGrid.BeginInit();
                this.SupplyCrateItemsGrid.BeginInit();
                this.StackSizeOverrideGrid.BeginInit();

                this.BasePrimalItemList = newList;
            }
            finally
            {
                this.CraftingOverrideItemGrid.EndInit();
                this.CraftingOverrideResourceGrid.EndInit();
                this.SupplyCrateItemsGrid.EndInit();
                this.StackSizeOverrideGrid.EndInit();
            }
        }

        public void RefreshBaseSupplyCrateList()
        {
            var newList = new ComboBoxItemList();

            foreach (var primalItem in GameData.GetSupplyCrates())
            {
                newList.Add(new Common.Model.ComboBoxItem
                {
                    DisplayMember = string.IsNullOrWhiteSpace(primalItem.Mod) ? $"{primalItem.DisplayName}" : $"({primalItem.DisplayMod}) {primalItem.DisplayName}",
                    ValueMember = primalItem.ClassName,
                });
            }

            newList.Sort(i => $"{i.GroupMember}||{i.DisplayMember}");

            foreach (var supplyCrate in this.Settings.ConfigOverrideSupplyCrateItems)
            {
                if (!newList.Any(s => s.ValueMember.Equals(supplyCrate.SupplyCrateClassString)))
                {
                    if (string.IsNullOrWhiteSpace(supplyCrate.SupplyCrateClassString))
                        continue;

                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = supplyCrate.SupplyCrateClassString,
                        ValueMember = supplyCrate.SupplyCrateClassString,
                    });
                }
            }

            try
            {
                this.SupplyCratesGrid.BeginInit();

                this.BaseSupplyCrateList = newList;
            }
            finally
            {
                this.SupplyCratesGrid.EndInit();
            }
        }

        public void RefreshBaseGameMapsList()
        {
            var newList = new ComboBoxItemList();

            if (this.Settings.SOTF_Enabled)
            {
                foreach (var item in GameData.GetGameMapsSotF())
                {
                    item.DisplayMember = GameData.FriendlyMapSotFNameForClass(item.ValueMember);
                    newList.Add(item);
                }
            }
            else
            {
                foreach (var item in GameData.GetGameMaps())
                {
                    item.DisplayMember = GameData.FriendlyMapNameForClass(item.ValueMember);
                    newList.Add(item);
                }
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.ServerMap))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.ServerMap)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = this.Settings.SOTF_Enabled ? GameData.FriendlyMapSotFNameForClass(this.Settings.ServerMap) : GameData.FriendlyMapNameForClass(this.Settings.ServerMap),
                        ValueMember = this.Settings.ServerMap,
                    });
                }
            }

            this.BaseGameMaps = newList;
            this.GameMapComboBox.SelectedValue = this.Settings.ServerMap;
        }

        public void RefreshBaseTotalConversionsList()
        {
            var newList = new ComboBoxItemList();

            if (this.Settings.SOTF_Enabled)
            {
                foreach (var item in GameData.GetTotalConversionsSotF())
                {
                    item.DisplayMember = GameData.FriendlyTotalConversionSotFNameForClass(item.ValueMember);
                    newList.Add(item);
                }
            }
            else
            {
                foreach (var item in GameData.GetTotalConversions())
                {
                    item.DisplayMember = GameData.FriendlyTotalConversionNameForClass(item.ValueMember);
                    newList.Add(item);
                }
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.TotalConversionModId))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.TotalConversionModId)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = this.Settings.SOTF_Enabled ? GameData.FriendlyTotalConversionSotFNameForClass(this.Settings.TotalConversionModId) : GameData.FriendlyTotalConversionNameForClass(this.Settings.TotalConversionModId),
                        ValueMember = this.Settings.TotalConversionModId,
                    });
                }
            }

            this.BaseTotalConversions = newList;
            this.TotalConversionComboBox.SelectedValue = this.Settings.TotalConversionModId;
        }

        public void RefreshBaseBranchesList()
        {
            var newList = new ComboBoxItemList();

            if (this.Settings.SOTF_Enabled)
            {
                foreach (var item in GameData.GetBranchesSotF())
                {
                    item.DisplayMember = GameData.FriendlyBranchSotFName(string.IsNullOrWhiteSpace(item.ValueMember) ? Config.Default.DefaultServerBranchName : item.ValueMember);
                    newList.Add(item);
                }
            }
            else
            {
                foreach (var item in GameData.GetBranches())
                {
                    item.DisplayMember = GameData.FriendlyBranchName(string.IsNullOrWhiteSpace(item.ValueMember) ? Config.Default.DefaultServerBranchName : item.ValueMember);
                    newList.Add(item);
                }
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.BranchName))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.BranchName)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = this.Settings.SOTF_Enabled ? GameData.FriendlyBranchSotFName(this.Settings.BranchName) : GameData.FriendlyBranchName(this.Settings.BranchName),
                        ValueMember = this.Settings.BranchName,
                    });
                }
            }

            this.BaseBranches = newList;
            this.BranchComboBox.SelectedValue = this.Settings.BranchName;
        }

        public void RefreshBaseEventsList()
        {
            var newList = new ComboBoxItemList();

            if (this.Settings.SOTF_Enabled)
            {
                foreach (var item in GameData.GetEventsSotF())
                {
                    item.DisplayMember = GameData.FriendlyEventSotFName(string.IsNullOrWhiteSpace(item.ValueMember) ? string.Empty : item.ValueMember);
                    newList.Add(item);
                }
            }
            else
            {
                foreach (var item in GameData.GetEvents())
                {
                    item.DisplayMember = GameData.FriendlyEventName(string.IsNullOrWhiteSpace(item.ValueMember) ? string.Empty : item.ValueMember);
                    newList.Add(item);
                }
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.EventName))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.EventName)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = this.Settings.SOTF_Enabled ? GameData.FriendlyEventSotFName(this.Settings.EventName) : GameData.FriendlyEventName(this.Settings.EventName),
                        ValueMember = this.Settings.EventName,
                    });
                }
            }

            this.BaseEvents = newList;
            this.EventComboBox.SelectedValue = this.Settings.EventName;
        }

        public void RefreshProcessPrioritiesList()
        {
            var newList = new ComboBoxItemList();

            foreach (var priority in ProcessUtils.GetProcessPriorityList())
            {
                newList.Add(new Common.Model.ComboBoxItem(priority, _globalizer.GetResourceString($"Priority_{priority}")));
            }

            var profilePriority = this.Settings.ProcessPriority;

            this.ProcessPriorities = newList;
            this.ProcessPriorityComboBox.SelectedValue = profilePriority;
        }

        public void RefreshCustomLevelProgressionsInformation()
        {
            var information = _globalizer.GetResourceString("ServerSettings_CustomLevelProgressions_InformationLabel");
            CustomLevelProgressionsInformation = information.Replace("{levels}", GameData.LevelsPlayerAdditional.ToString());
        }

        private void ReinitializeNetworkAdapters()
        {
            var adapters = NetworkUtils.GetAvailableIPV4NetworkAdapters();

            //
            // Filter out self-assigned addresses
            //
            adapters.RemoveAll(a => a.IPAddress.StartsWith("169.254."));
            adapters.Insert(0, new NetworkAdapterEntry(String.Empty, _globalizer.GetResourceString("ServerSettings_LocalIPGameChooseLabel")));
            var savedServerIp = this.Settings.ServerIP;
            this.NetworkInterfaces = adapters;
            this.Settings.ServerIP = savedServerIp;


            if (!String.IsNullOrWhiteSpace(this.Settings.ServerIP))
            {
                if (adapters.FirstOrDefault(a => String.Equals(a.IPAddress, this.Settings.ServerIP, StringComparison.OrdinalIgnoreCase)) == null)
                {
                    MessageBox.Show(
                        String.Format(_globalizer.GetResourceString("ServerSettings_LocalIP_ErrorLabel"), this.Settings.ServerIP),
                        _globalizer.GetResourceString("ServerSettings_LocalIP_ErrorTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        public ICommand ResetActionCommand
        {
            get
            {
                return new RelayCommand<ServerSettingsResetAction>(
                    execute: (action) =>
                    {
                        if (action != ServerSettingsResetAction.MapNameTotalConversionProperty)
                        {
                            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ResetLabel"), _globalizer.GetResourceString("ServerSettings_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                                return;
                        }

                        switch (action)
                        {
                            // sections
                            case ServerSettingsResetAction.AdministrationSection:
                                this.Settings.ResetAdministrationSection();
                                RefreshBaseGameMapsList();
                                RefreshBaseTotalConversionsList();
                                RefreshCultureList();
                                RefreshBaseBranchesList();
                                RefreshBaseEventsList();
                                RefreshProcessPrioritiesList();
                                break;

                            case ServerSettingsResetAction.DiscordBotSection:
                                this.Settings.ResetDiscordBotSection();
                                break;

                            case ServerSettingsResetAction.ChatAndNotificationsSection:
                                this.Settings.ResetChatAndNotificationSection();
                                break;

                            case ServerSettingsResetAction.CraftingOverridesSection:
                                this.Settings.ResetCraftingOverridesSection();
                                RefreshBasePrimalItemList();
                                break;

                            case ServerSettingsResetAction.CustomLevelsSection:
                                this.Settings.ResetCustomLevelsSection();
                                break;

                            case ServerSettingsResetAction.DinoSettingsSection:
                                this.Settings.ResetDinoSettingsSection();
                                RefreshBaseDinoList();
                                break;

                            case ServerSettingsResetAction.EngramsSection:
                                this.Settings.ResetEngramsSection();
                                break;

                            case ServerSettingsResetAction.EnvironmentSection:
                                this.Settings.ResetEnvironmentSection();
                                break;

                            case ServerSettingsResetAction.HudAndVisualsSection:
                                this.Settings.ResetHUDAndVisualsSection();
                                break;

                            case ServerSettingsResetAction.MapSpawnerOverridesSection:
                                this.Settings.ResetNPCSpawnOverridesSection();
                                RefreshBaseMapSpawnerList();
                                RefreshBaseDinoList();
                                break;

                            case ServerSettingsResetAction.PGMSection:
                                this.Settings.ResetPGMSection();
                                break;

                            case ServerSettingsResetAction.PlayerSettingsSection:
                                this.Settings.ResetPlayerSettings();
                                break;

                            case ServerSettingsResetAction.RulesSection:
                                this.Settings.ResetRulesSection();
                                break;

                            case ServerSettingsResetAction.ServerDetailsSection:
                                this.Settings.ResetServerDetailsSection();
                                break;

                            case ServerSettingsResetAction.SOTFSection:
                                this.Settings.ResetSOTFSection();
                                break;

                            case ServerSettingsResetAction.StructuresSection:
                                this.Settings.ResetStructuresSection();
                                break;

                            case ServerSettingsResetAction.SupplyCrateOverridesSection:
                                this.Settings.ResetSupplyCrateOverridesSection();
                                RefreshBaseSupplyCrateList();
                                RefreshBasePrimalItemList();
                                break;

                            case ServerSettingsResetAction.ExcludeItemIndicesOverridesSection:
                                this.Settings.ResetExcludeItemIndicesOverridesSection();
                                RefreshBasePrimalItemList();
                                break;

                            case ServerSettingsResetAction.StackSizeOverridesSection:
                                this.Settings.ResetStackSizeOverridesSection();
                                RefreshBasePrimalItemList();
                                break;

                            case ServerSettingsResetAction.PreventTransferOverridesSection:
                                this.Settings.ResetPreventTransferOverridesSection();
                                RefreshBaseDinoList();
                                break;

                            // Properties
                            case ServerSettingsResetAction.MapNameTotalConversionProperty:
                                // set the map name to the ARK default.
                                var mapName = string.Empty;

                                // check if we are running an official total conversion mod.
                                if (!ModUtils.IsOfficialMod(this.Settings.TotalConversionModId))
                                {
                                    // we need to read the mod file and retreive the map name
                                    mapName = ModUtils.GetMapName(this.Settings.InstallDirectory, this.Settings.TotalConversionModId);
                                    if (string.IsNullOrWhiteSpace(mapName))
                                    {
                                        MessageBox.Show(_globalizer.GetResourceString("ServerSettings_FindTotalConversionMapNameErrorLabel"), _globalizer.GetResourceString("ServerSettings_FindTotalConversionMapNameErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                                        break;
                                    }
                                }

                                this.Settings.ServerMap = mapName;

                                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_FindTotalConversionMapNameSuccessLabel"), _globalizer.GetResourceString("ServerSettings_FindTotalConversionMapNameSuccessTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                                break;

                            case ServerSettingsResetAction.BanListProperty:
                                this.Settings.ResetBanlist();
                                break;

                            case ServerSettingsResetAction.PlayerMaxXpProperty:
                                this.Settings.ResetOverrideMaxExperiencePointsPlayer();
                                break;

                            case ServerSettingsResetAction.DinoMaxXpProperty:
                                this.Settings.ResetOverrideMaxExperiencePointsDino();
                                break;

                            case ServerSettingsResetAction.PlayerBaseStatMultipliers:
                                this.Settings.PlayerBaseStatMultipliers.Reset();
                                break;

                            case ServerSettingsResetAction.PlayerPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_Player.Reset();
                                break;

                            case ServerSettingsResetAction.DinoWildPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_DinoWild.Reset();
                                break;

                            case ServerSettingsResetAction.DinoTamedPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_DinoTamed.Reset();
                                break;

                            case ServerSettingsResetAction.DinoTamedAddPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_DinoTamed_Add.Reset();
                                break;

                            case ServerSettingsResetAction.DinoTamedAffinityPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_DinoTamed_Affinity.Reset();
                                break;

                            case ServerSettingsResetAction.DinoWildMutagenLevelBoost:
                                this.Settings.MutagenLevelBoost.Reset();
                                break;

                            case ServerSettingsResetAction.DinoBredMutagenLevelBoost:
                                this.Settings.MutagenLevelBoost_Bred.Reset();
                                break;

                            case ServerSettingsResetAction.ItemStatClamps:
                                break;

                            case ServerSettingsResetAction.RCONWindowExtents:
                                this.Settings.ResetRCONWindowExtents();
                                break;

                            case ServerSettingsResetAction.ServerOptions:
                                this.Settings.ResetServerOptions();
                                break;

                            case ServerSettingsResetAction.ServerLogOptions:
                                this.Settings.ResetServerLogOptions();
                                break;

                            case ServerSettingsResetAction.ServerBadWordFilterOptions:
                                this.Settings.ResetServerBadWordFilterOptions();
                                break;
                        }
                    },
                    canExecute: (action) => true
                );
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: async (parameter) =>
                    {
                        try
                        {
                            dockPanel.IsEnabled = false;
                            OverlayMessage.Content = _globalizer.GetResourceString("ServerSettings_OverlayMessage_SavingLabel");
                            OverlayGrid.Visibility = Visibility.Visible;

                            await Task.Delay(100);

                            // NOTE: This parameter is of type object and must be cast in most cases before use.
                            var server = parameter as Server;
                            if (server != null)
                            {
                                if (server.Profile.EnableAutoShutdown1 || server.Profile.EnableAutoShutdown2)
                                {
                                    if (server.Profile.SOTF_Enabled)
                                    {
                                        MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_AutoRestart_SotF_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_AutoRestart_SotF_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                        server.Profile.EnableAutoShutdown1 = false;
                                        server.Profile.RestartAfterShutdown1 = true;
                                        server.Profile.EnableAutoShutdown2 = false;
                                        server.Profile.RestartAfterShutdown2 = true;
                                        server.Profile.AutoRestartIfShutdown = false;
                                    }
                                }

                                if (server.Profile.EnableAutoUpdate)
                                {
                                    if (server.Profile.SOTF_Enabled)
                                    {
                                        MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_AutoUpdate_SotF_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_AutoUpdate_SotF_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                        server.Profile.EnableAutoUpdate = false;
                                        server.Profile.AutoRestartIfShutdown = false;
                                    }
                                }

                                server.Profile.Save(false, false, (p, m, n) => { OverlayMessage.Content = m; });

                                RefreshBaseDinoList();
                                RefreshBaseMapSpawnerList();
                                RefreshBasePrimalItemList();
                                RefreshBaseSupplyCrateList();
                                RefreshBaseGameMapsList();
                                RefreshBaseTotalConversionsList();
                                RefreshCultureList();
                                RefreshBaseBranchesList();
                                RefreshBaseEventsList();
                                RefreshProcessPrioritiesList();

                                if (Config.Default.UpdateDirectoryPermissions)
                                {
                                    OverlayMessage.Content = _globalizer.GetResourceString("ServerSettings_OverlayMessage_PermissionsLabel");
                                    await Task.Delay(100);

                                    server.Profile.UpdateDirectoryPermissions();
                                }

                                OverlayMessage.Content = _globalizer.GetResourceString("ServerSettings_OverlayMessage_SchedulesLabel");
                                await Task.Delay(100);

                                if (!server.Profile.UpdateSchedules())
                                {
                                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_UpdateSchedule_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_UpdateSchedule_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            OverlayGrid.Visibility = Visibility.Collapsed;
                            dockPanel.IsEnabled = true;
                        }
                    },
                    canExecute: (parameter) =>
                    {
                        return (parameter as Server) != null;
                    }
                );
            }
        }

        public bool SelectControl(Control control)
        {
            if (control is null || control.Visibility != Visibility.Visible)
                return false;

            bool focused = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                Window.GetWindow(this)?.Activate();

                UnselectControl();

                var parent = WindowUtils.TryFindParent<Expander>(control);
                if (parent != null)
                {
                    parent.IsExpanded = true;
                }

                var item = new FindSettingItem()
                {
                    FoundControl = control,
                    BackgroundBrush = control.Background,
                };

                control.Background = (Brush)FindResource("FoundSetting");
                control.BringIntoView();

                if (control is AnnotatedSlider)
                {
                    focused = ((AnnotatedSlider)control).Focus();
                }
                else if (control is AnnotatedCheckBoxAndFloatSlider)
                {
                    focused = ((AnnotatedCheckBoxAndFloatSlider)control).Focus();
                }
                else if (control is AnnotatedCheckBoxAndIntegerSlider)
                {
                    focused = ((AnnotatedCheckBoxAndIntegerSlider)control).Focus();
                }
                else if (control is AnnotatedCheckBoxAndLongSlider)
                {
                    focused = ((AnnotatedCheckBoxAndLongSlider)control).Focus();
                }
                else if (control is CheckBoxAndTextBlock)
                {
                    focused = ((CheckBoxAndTextBlock)control).Focus();
                }
                else
                {
                    focused = control.Focus();
                }

                _lastFoundSetting = item;
            });

            return true;
        }

        public void UnselectControl()
        {
            if (_lastFoundSetting is null)
                return;

            _lastFoundSetting.FoundControl.Background = _lastFoundSetting.BackgroundBrush;
            _lastFoundSetting = null;
        }

        private async Task<bool> UpdateServer(bool establishLock, bool updateServer, bool updateMods, bool closeProgressWindow)
        {
            if (_upgradeCancellationSource != null)
                return false;

            ProgressWindow window = null;
            Mutex mutex = null;
            bool createdNew = !establishLock;

            try
            {
                if (establishLock)
                {
                    // try to establish a mutex for the profile.
                    mutex = new Mutex(true, ServerApp.GetMutexName(this.Server.Profile.InstallDirectory), out createdNew);
                }

                // check if the mutex was established
                if (createdNew)
                {
                    this._upgradeCancellationSource = new CancellationTokenSource();

                    window = new ProgressWindow(string.Format(_globalizer.GetResourceString("Progress_UpgradeServer_WindowTitle"), this.Server.Profile.ProfileName));
                    window.Owner = Window.GetWindow(this);
                    window.Closed += Window_Closed;
                    window.Show();

                    await Task.Delay(1000);

                    var branch = BranchSnapshot.Create(this.Server.Profile);
                    return await this.Server.UpgradeAsync(_upgradeCancellationSource.Token, updateServer, branch, true, updateMods, (p, m, n) => { TaskUtils.RunOnUIThreadAsync(() => { window?.AddMessage(m, n); }).DoNotWait(); });
                }
                else
                {
                    // display an error message and exit
                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_UpgradeServer_MutexFailedLabel"), _globalizer.GetResourceString("ServerSettings_UpgradeServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (window != null)
                {
                    window.AddMessage(ex.Message);
                    window.AddMessage(ex.StackTrace);
                }
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_UpgradeServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                this._upgradeCancellationSource = null;

                if (window != null)
                {
                    window.CloseWindow();
                    if (closeProgressWindow)
                        window.Close();
                }

                if (mutex != null)
                {
                    if (createdNew)
                    {
                        mutex.ReleaseMutex();
                        mutex.Dispose();
                    }
                    mutex = null;
                }
            }
        }

        public void UpdateLastStartedDetails(bool updateProfile)
        {
            if (updateProfile)
            {
                // update the profile's last started time
                this.Settings.LastStarted = DateTime.Now;
                this.Settings.SaveProfile();
            }

            var date = Settings == null || Settings.LastStarted == DateTime.MinValue ? string.Empty : $"{Settings.LastStarted:G}";
            this.ProfileLastStarted = $"{_globalizer.GetResourceString("ServerSettings_LastStartedLabel")} {date}";
        }
        #endregion
    }
}
