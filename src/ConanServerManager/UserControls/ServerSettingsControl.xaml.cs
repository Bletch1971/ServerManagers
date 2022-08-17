using Microsoft.WindowsAPICodePack.Dialogs;
using ServerManagerTool.Common;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using ServerManagerTool.Plugin.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    partial class ServerSettingsControl : UserControl
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private CancellationTokenSource _upgradeCancellationSource = null;

        // Using a DependencyProperty as the backing store for ServerManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BaseGameMapsProperty = DependencyProperty.Register(nameof(BaseGameMaps), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseBranchesProperty = DependencyProperty.Register(nameof(BaseBranches), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ConfigProperty = DependencyProperty.Register(nameof(Config), typeof(Config), typeof(ServerSettingsControl));
        public static readonly DependencyProperty IsAdministratorProperty = DependencyProperty.Register(nameof(IsAdministrator), typeof(bool), typeof(ServerSettingsControl), new PropertyMetadata(false));
        public static readonly DependencyProperty NetworkInterfacesProperty = DependencyProperty.Register(nameof(NetworkInterfaces), typeof(List<NetworkAdapterEntry>), typeof(ServerSettingsControl), new PropertyMetadata(new List<NetworkAdapterEntry>()));
        public static readonly DependencyProperty RuntimeProperty = DependencyProperty.Register(nameof(Runtime), typeof(ServerRuntime), typeof(ServerSettingsControl));
        public static readonly DependencyProperty ServerManagerProperty = DependencyProperty.Register(nameof(ServerManager), typeof(ServerManager), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ServerProperty = DependencyProperty.Register(nameof(Server), typeof(Server), typeof(ServerSettingsControl), new PropertyMetadata(null, ServerPropertyChanged));
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(ServerProfile), typeof(ServerSettingsControl));
        public static readonly DependencyProperty ProcessPrioritiesProperty = DependencyProperty.Register(nameof(ProcessPriorities), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ServerRegionsProperty = DependencyProperty.Register(nameof(ServerRegions), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty CurrentCultureProperty = DependencyProperty.Register(nameof(CurrentCulture), typeof(CultureInfo), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty DisplayModInformationProperty = DependencyProperty.Register(nameof(DisplayModInformation), typeof(bool), typeof(ServerSettingsControl), new PropertyMetadata(false));
        public static readonly DependencyProperty ProfileLastStartedProperty = DependencyProperty.Register(nameof(ProfileLastStarted), typeof(string), typeof(ServerSettingsControl), new PropertyMetadata(""));

        #region Properties
        public ComboBoxItemList BaseGameMaps
        {
            get { return (ComboBoxItemList)GetValue(BaseGameMapsProperty); }
            set { SetValue(BaseGameMapsProperty, value); }
        }

        public ComboBoxItemList BaseBranches
        {
            get { return (ComboBoxItemList)GetValue(BaseBranchesProperty); }
            set { SetValue(BaseBranchesProperty, value); }
        }

        public Config Config
        {
            get { return GetValue(ConfigProperty) as Config; }
            set { SetValue(ConfigProperty, value); }
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

        public ComboBoxItemList ProcessPriorities
        {
            get { return (ComboBoxItemList)GetValue(ProcessPrioritiesProperty); }
            set { SetValue(ProcessPrioritiesProperty, value); }
        }

        public ComboBoxItemList ServerRegions
        {
            get { return (ComboBoxItemList)GetValue(ServerRegionsProperty); }
            set { SetValue(ServerRegionsProperty, value); }
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

        public string ProfileLastStarted
        {
            get { return (string)GetValue(ProfileLastStartedProperty); }
            set { SetValue(ProfileLastStartedProperty, value); }
        }
        #endregion

        public ServerSettingsControl()
        {
            this.Config = Config.Default;
            this.CurrentCulture = Thread.CurrentThread.CurrentCulture;

            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.ServerManager = ServerManager.Instance;
            this.IsAdministrator = SecurityUtils.IsAdministrator();
            this.DisplayModInformation = !string.IsNullOrWhiteSpace(SteamUtils.SteamWebApiKey);

            this.BaseGameMaps = new ComboBoxItemList();
            this.BaseBranches = new ComboBoxItemList();
            this.ProcessPriorities = new ComboBoxItemList();
            this.ServerRegions = new ComboBoxItemList();

            UpdateLastStartedDetails(false);

            // hook into the language change event
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent += ResourceDictionaryChangedEvent;
            GameData.GameDataLoaded += GameData_GameDataLoaded;
        }

        #region Event Methods
        private void GameData_GameDataLoaded(object sender, EventArgs e)
        {
            this.RefreshBaseGameMapsList();
            this.RefreshBaseBranchesList();
            this.RefreshProcessPrioritiesList();
            this.RefreshServerRegionsList();
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
                        ssc.RefreshBaseGameMapsList();
                        ssc.RefreshBaseBranchesList();
                        ssc.RefreshProcessPrioritiesList();
                        ssc.RefreshServerRegionsList();
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
            }
        }

        private void ModDetailsWindow_SavePerformed(object sender, ProfileEventArgs e)
        {
            if (sender is ModDetailsWindow && Equals(e.Profile, Settings))
            {
                RefreshBaseGameMapsList();
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

                            if (!this.Server.Profile.Validate(false, out string validateMessage))
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

            try
            {
                this.Settings.Save(false, false, null);
                await UpdateServer(true, true, Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_UpdateServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void OpenRcon_Click(object sender, RoutedEventArgs e)
        {
            var window = RconWindow.GetRconForServer(this.Server);
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
            var window = new ModDetailsWindow(this.Server.Profile)
            {
                Owner = Window.GetWindow(this)
            };
            window.Closed += Window_Closed;
            window.SavePerformed += ModDetailsWindow_SavePerformed;
            window.Show();
            window.Focus();
        }

        private void PatchNotes_Click(object sender, RoutedEventArgs e)
        {
            var url = this.Settings.UseTestlive ? Config.Default.AppPatchNotesUrl_Testlive : Config.Default.AppPatchNotesUrl;
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
                logFolder = Config.Default.DataPath;
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

                // <server>\ConanSandbox\mods
                var folder = Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerModsRelativePath);
                var dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.txt").Select(timeFile => timeFile.FullName));

                    var modListFile = Path.Combine(folder, Config.Default.ServerModListFile);
                    if (File.Exists(modListFile) && !files.Contains(modListFile))
                        files.Add(modListFile);
                }

                // <server>\ConanSandbox\Saved\Config\WindowsServer
                file = Path.Combine(this.Settings.GetProfileServerConfigDir(), Config.Default.ServerEngineConfigFile);
                if (File.Exists(file))
                {
                    var iniFile = IniFileUtils.ReadFromFile(file);
                    if (iniFile != null)
                    {
                        iniFile.WriteKey("OnlineSubsystem", "ServerPassword", "obfuscated");
                        iniFile.WriteKey("OnlineSubsystemSteam", "ServerPassword", "obfuscated");

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
                file = Path.Combine(this.Settings.GetProfileServerConfigDir(), Config.Default.ServerGameConfigFile);
                if (File.Exists(file))
                {
                    var iniFile = IniFileUtils.ReadFromFile(file);
                    if (iniFile != null)
                    {
                        iniFile.WriteKey("RconPlugin", "RconPassword", "obfuscated");

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
                file = Path.Combine(this.Settings.GetProfileServerConfigDir(), Config.Default.ServerSettingsConfigFile);
                if (File.Exists(file))
                {
                    var iniFile = IniFileUtils.ReadFromFile(file);
                    if (iniFile != null)
                    {
                        iniFile.WriteKey("ServerSettings", "AdminPassword", "obfuscated");

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
                folder = Path.Combine(Config.Default.DataPath, Config.Default.LogsRelativePath);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                folder = Path.Combine(Config.Default.DataPath, Config.Default.LogsRelativePath, ServerApp.LOGPREFIX_AUTOBACKUP);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                folder = Path.Combine(Config.Default.DataPath, Config.Default.LogsRelativePath, ServerApp.LOGPREFIX_AUTOSHUTDOWN);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                folder = Path.Combine(Config.Default.DataPath, Config.Default.LogsRelativePath, ServerApp.LOGPREFIX_AUTOUPDATE);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                // Logs/<server>
                folder = Path.Combine(Config.Default.DataPath, Config.Default.LogsRelativePath, this.Settings.ProfileID.ToLower());
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
                        profileFile.RconPassword = string.IsNullOrWhiteSpace(profileFile.RconPassword) ? "empty" : "obfuscated";
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
                var appId = this.Settings.UseTestlive ? Config.Default.AppId_Testlive : Config.Default.AppId;
                var workshopPath = string.Format(Config.Default.AppSteamWorkshopFolderRelativePath, appId);
                folder = Path.Combine(Config.Default.DataPath, CommonConfig.Default.SteamCmdRelativePath, workshopPath);
                if (Directory.Exists(folder))
                {
                    foreach (var modFolder in Directory.GetDirectories(folder))
                    {
                        file = Path.Combine(modFolder, Config.Default.LastUpdatedTimeFile);
                        if (File.Exists(file)) files.Add(file);
                    }
                }

                // <server cache>
                if (!string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_CacheDir))
                {
                    var branchName = string.IsNullOrWhiteSpace(this.Settings.BranchName) ? Config.Default.DefaultServerBranchName : this.Settings.BranchName;
                    file = IOUtils.NormalizePath(Path.Combine(Config.Default.AutoUpdate_CacheDir, $"{Config.Default.ServerBranchFolderPrefix}{branchName}", Config.Default.LastUpdatedTimeFile));
                    if (File.Exists(file)) files.Add(file);
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
                taskKey = TaskSchedulerUtils.ComputeKey(Config.Default.DataPath);

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
                comment.AppendLine($"Data Directory: {Config.Default.DataPath}");
                comment.AppendLine($"Profiles Directory: {Config.Default.ConfigPath}");
                comment.AppendLine($"Server Directory: {this.Settings.InstallDirectory}");

                comment.AppendLine($"Testlive Server: {this.Settings.UseTestlive}");

                comment.AppendLine($"IsAdministrator: {SecurityUtils.IsAdministrator()}");
                comment.AppendLine($"RunAsAdministratorPrompt: {Config.Default.RunAsAdministratorPrompt}");
                comment.AppendLine($"CheckIfServerManagerRunningOnStartup: {Config.Default.CheckIfServerManagerRunningOnStartup}");
                comment.AppendLine($"ManageFirewallAutomatically: {Config.Default.ManageFirewallAutomatically}");
                comment.AppendLine($"ManagePublicIPAutomatically: {Config.Default.ManagePublicIPAutomatically}");
                comment.AppendLine($"MainWindow_WindowState: {Config.Default.MainWindow_WindowState}");
                comment.AppendLine($"MainWindow_MinimizeToTray: {Config.Default.MainWindow_MinimizeToTray}");
                comment.AppendLine($"ServerMonitorWindow_WindowState: {Config.Default.ServerMonitorWindow_WindowState}");

                comment.AppendLine($"SteamCMD File: {SteamCmdUpdater.GetSteamCmdFile(Config.Default.DataPath)}");
                comment.AppendLine($"SteamCmd_UseAnonymousCredentials: {Config.Default.SteamCmd_UseAnonymousCredentials}");
                comment.AppendLine($"SteamCmd_Username Set: {!string.IsNullOrWhiteSpace(Config.Default.SteamCmd_Username)}");
                comment.AppendLine($"SteamCmd_Password Set: {!string.IsNullOrWhiteSpace(Config.Default.SteamCmd_Password)}");
                comment.AppendLine($"SteamAPIKey: {!string.IsNullOrWhiteSpace(CommonConfig.Default.SteamAPIKey)}");

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
                    comment.AppendLine($"Backup Directory: *{Path.Combine(Config.Default.DataPath, Config.Default.BackupRelativePath)}");
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

                var result = this.Settings.Validate(true, out string validationMessage);

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
            var dialog = new CommonOpenFileDialog()
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
                Settings.ServerMapSaveFileName = string.Empty;

                Settings.ChangeInstallationFolder(dialog.FileName, reloadConfigFiles: true);
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Config.Default.ConfigPath))
            {
                Directory.CreateDirectory(Config.Default.ConfigPath);
            }

            var dialog = new CommonOpenFileDialog()
            {
                EnsureFileExists = true,
                InitialDirectory = Config.Default.ConfigPath,
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
            var cmdLine = new CommandLineWindow(String.Format("{0} {1}", this.Runtime.GetServerExe(), this.Settings.GetServerArgs()))
            {
                Owner = Window.GetWindow(this)
            };
            cmdLine.ShowDialog();
        }

        private void ServerAutoSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ServerAutoSettings_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_ServerAutoSettings_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
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
            var window = new WorldSaveRestoreWindow(Server.Profile)
            {
                Owner = Window.GetWindow(this)
            };
            window.Closed += Window_Closed;
            window.ShowDialog();
        }

        private void HiddenField_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox hideTextBox)
            {
                TextBox textBox = null;
                if (Equals(hideTextBox, HideServerPasswordTextBox))
                    textBox = ServerPasswordTextBox;
                if (Equals(hideTextBox, HideAdminPasswordTextBox))
                    textBox = AdminPasswordTextBox;
                if (Equals(hideTextBox, HideRconPasswordTextBox))
                    textBox = RconPasswordTextBox;
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
            if (sender is TextBox textBox)
            {
                TextBox hideTextBox = null;
                if (textBox == ServerPasswordTextBox)
                    hideTextBox = HideServerPasswordTextBox;
                if (textBox == AdminPasswordTextBox)
                    hideTextBox = HideAdminPasswordTextBox;
                if (textBox == RconPasswordTextBox)
                    hideTextBox = HideRconPasswordTextBox;
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

        private void MapName_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.SyncMapSaveFileName();
        }

        private void MOTD_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.ValidateMOTD();
        }

        private void SyncProfile_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProfileSyncWindow(ServerManager, Server.Profile)
            {
                Owner = Window.GetWindow(this)
            };
            window.Closed += Window_Closed;
            window.ShowDialog();
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

        #region Server Files 
        private void AddBlacklistedPlayer_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddUserWindow()
            {
                Owner = Window.GetWindow(this)
            };
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
                    Settings.ServerFilesBlacklisted.AddRange(steamUserList);

                    Settings.SaveServerFileBlacklisted();
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

        private void AddWhitelistedPlayer_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddUserWindow()
            {
                Owner = Window.GetWindow(this)
            };
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

        private void ClearBlacklistedPlayers_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                Settings.DestroyServerFilesWatcher();

                Settings.ServerFilesBlacklisted.Clear();
                Settings.SaveServerFileBlacklisted();
            }
            finally
            {
                Settings.SetupServerFilesWatcher();
            }
        }

        private void ClearWhitelistedPlayers_Click(object sender, RoutedEventArgs e)
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

        private async void ReloadBlacklistedPlayers_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                Settings.LoadServerFiles(true, false);
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

        private async void ReloadWhitelistedPlayers_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                Settings.LoadServerFiles(false, true);
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

        private void RemoveBlacklistedPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                Settings.DestroyServerFilesWatcher();

                var mod = ((PlayerUserItem)((Button)e.Source).DataContext);
                Settings.ServerFilesBlacklisted.Remove(mod.PlayerId);

                Settings.SaveServerFileBlacklisted();
            }
            finally
            {
                Settings.SetupServerFilesWatcher();
            }
        }

        private void RemoveWhitelistedPlayer_Click(object sender, RoutedEventArgs e)
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

        #endregion

        #region Methods
        public void CloseControl()
        {
            GameData.GameDataLoaded -= GameData_GameDataLoaded;
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent -= ResourceDictionaryChangedEvent;
        }

        public void RefreshBaseGameMapsList()
        {
            var newList = new ComboBoxItemList();

            foreach (var item in GameData.GetGameMaps())
            {
                item.DisplayMember = GameData.FriendlyMapNameForClass(item.ValueMember);
                newList.Add(item);
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.ServerMap))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.ServerMap)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = GameData.FriendlyMapNameForClass(this.Settings.ServerMap),
                        ValueMember = this.Settings.ServerMap,
                    });
                }
            }

            var profileServerMap = this.Settings.ServerMap;

            this.BaseGameMaps = newList;
            this.GameMapComboBox.SelectedValue = profileServerMap;
        }

        public void RefreshBaseBranchesList()
        {
            var newList = new ComboBoxItemList();

            foreach (var item in GameData.GetBranches())
            {
                item.DisplayMember = GameData.FriendlyBranchName(string.IsNullOrWhiteSpace(item.ValueMember) ? Config.Default.DefaultServerBranchName : item.ValueMember);
                newList.Add(item);
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.BranchName))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.BranchName)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = GameData.FriendlyBranchName(this.Settings.BranchName),
                        ValueMember = this.Settings.BranchName,
                    });
                }
            }

            var profileBranchName = this.Settings.BranchName;

            this.BaseBranches = newList;
            this.BranchComboBox.SelectedValue = profileBranchName;
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

        public void RefreshServerRegionsList()
        {
            var newList = new ComboBoxItemList();

            foreach (var item in GameData.GetServerRegions())
            {
                item.DisplayMember = GameData.FriendlyServerRegionName(item.ValueMember);
                newList.Add(item);
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.ServerRegion))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.ServerRegion)))
                {
                    newList.Add(new Common.Model.ComboBoxItem
                    {
                        DisplayMember = GameData.FriendlyServerRegionName(this.Settings.ServerRegion),
                        ValueMember = this.Settings.ServerRegion,
                    });
                }
            }

            var serverRegion = this.Settings.ServerRegion;

            this.ServerRegions = newList;
            this.ServerRegionsComboBox.SelectedValue = serverRegion;
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
                        if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ResetLabel"), _globalizer.GetResourceString("ServerSettings_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                            return;

                        switch (action)
                        {
                            // sections
                            case ServerSettingsResetAction.AdministrationSection:
                                this.Settings.ResetAdministrationSection();
                                RefreshBaseGameMapsList();
                                RefreshBaseBranchesList();
                                RefreshProcessPrioritiesList();
                                RefreshServerRegionsList();
                                break;

                            case ServerSettingsResetAction.DiscordBotSection:
                                this.Settings.ResetDiscordBotSection();
                                break;

                            // Properties
                            case ServerSettingsResetAction.RconWindowExtents:
                                this.Settings.ResetRconWindowExtents();
                                break;

                            case ServerSettingsResetAction.ServerDetailsSection:
                                this.Settings.ResetServerDetailsSection();
                                break;

                            case ServerSettingsResetAction.ServerOptions:
                                this.Settings.ResetServerOptions();
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
                                server.Profile.Save(false, false, (p, m, n) => { OverlayMessage.Content = m; });

                                RefreshBaseGameMapsList();
                                RefreshBaseBranchesList();
                                RefreshProcessPrioritiesList();
                                RefreshServerRegionsList();

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

                    window = new ProgressWindow(string.Format(_globalizer.GetResourceString("Progress_UpgradeServer_WindowTitle"), this.Server.Profile.ProfileName))
                    {
                        Owner = Window.GetWindow(this)
                    };
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