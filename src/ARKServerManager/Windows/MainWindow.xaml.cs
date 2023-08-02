using EO.Wpf;
using NLog;
using ServerManagerTool.Common;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib;
using ServerManagerTool.Plugin.Common;
using ServerManagerTool.Utils;
using ServerManagerTool.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private readonly ActionQueue versionChecker;
        private readonly ActionQueue scheduledTaskChecker;
        private readonly ActionQueue discordBotChecker;

        private bool discordBotStateClicked = false;

        public static readonly DependencyProperty AppInstanceProperty = DependencyProperty.Register(nameof(AppInstance), typeof(App), typeof(MainWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty ConfigProperty = DependencyProperty.Register(nameof(Config), typeof(Config), typeof(MainWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty CommonConfigProperty = DependencyProperty.Register(nameof(CommonConfig), typeof(CommonConfig), typeof(MainWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty ServerManagerProperty = DependencyProperty.Register(nameof(ServerManager), typeof(ServerManager), typeof(MainWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty AutoBackupStateProperty = DependencyProperty.Register(nameof(AutoBackupState), typeof(Microsoft.Win32.TaskScheduler.TaskState), typeof(MainWindow), new PropertyMetadata(Microsoft.Win32.TaskScheduler.TaskState.Unknown));
        public static readonly DependencyProperty AutoBackupStateStringProperty = DependencyProperty.Register(nameof(AutoBackupStateString), typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty AutoBackupNextRunTimeProperty = DependencyProperty.Register(nameof(AutoBackupNextRunTime), typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty AutoUpdateStateProperty = DependencyProperty.Register(nameof(AutoUpdateState), typeof(Microsoft.Win32.TaskScheduler.TaskState), typeof(MainWindow), new PropertyMetadata(Microsoft.Win32.TaskScheduler.TaskState.Unknown));
        public static readonly DependencyProperty AutoUpdateStateStringProperty = DependencyProperty.Register(nameof(AutoUpdateStateString), typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty AutoUpdateNextRunTimeProperty = DependencyProperty.Register(nameof(AutoUpdateNextRunTime), typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty DiscordBotStateProperty = DependencyProperty.Register(nameof(DiscordBotState), typeof(DiscordBot.Enums.BotState), typeof(MainWindow), new PropertyMetadata(DiscordBot.Enums.BotState.Unknown));
        public static readonly DependencyProperty DiscordBotStateStringProperty = DependencyProperty.Register(nameof(DiscordBotStateString), typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty IsIpValidProperty = DependencyProperty.Register(nameof(IsIpValid), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty LatestServerManagerVersionProperty = DependencyProperty.Register(nameof(LatestServerManagerVersion), typeof(Version), typeof(MainWindow), new PropertyMetadata(new Version()));
        public static readonly DependencyProperty NewServerManagerAvailableProperty = DependencyProperty.Register(nameof(NewServerManagerAvailable), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public MainWindow()
        {
            this.AppInstance = App.Instance;
            this.Config = Config.Default;
            this.CommonConfig = CommonConfig.Default;

            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.ServerManager = ServerManager.Instance;

            this.DataContext = this;
            this.versionChecker = new ActionQueue();
            this.scheduledTaskChecker = new ActionQueue();
            this.discordBotChecker = new ActionQueue();

            IsAdministrator = SecurityUtils.IsAdministrator();
            if (!string.IsNullOrWhiteSpace(App.Instance.Title))
            {
                this.Title = App.Instance.Title;
            }
            else
            {
                if (IsAdministrator)
                {
                    this.Title = _globalizer.GetResourceString("MainWindow_TitleWithAdmin");
                }
                else
                {
                    this.Title = _globalizer.GetResourceString("MainWindow_Title");
                }
            }

            this.Left = Config.Default.MainWindow_Left;
            this.Top = Config.Default.MainWindow_Top;
            this.Height = Config.Default.MainWindow_Height;
            this.Width = Config.Default.MainWindow_Width;
            this.WindowState = Config.Default.MainWindow_WindowState;

            // hook into the language change event
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent += ResourceDictionaryChangedEvent;
        }

        public App AppInstance
        {
            get { return GetValue(AppInstanceProperty) as App; }
            set { SetValue(AppInstanceProperty, value); }
        }

        public Config Config
        {
            get { return GetValue(ConfigProperty) as Config; }
            set { SetValue(ConfigProperty, value); }
        }

        public CommonConfig CommonConfig
        {
            get { return GetValue(CommonConfigProperty) as CommonConfig; }
            set { SetValue(CommonConfigProperty, value); }
        }

        public ServerManager ServerManager
        {
            get { return (ServerManager)GetValue(ServerManagerProperty); }
            set { SetValue(ServerManagerProperty, value); }
        }

        public Microsoft.Win32.TaskScheduler.TaskState AutoBackupState
        {
            get { return (Microsoft.Win32.TaskScheduler.TaskState)GetValue(AutoBackupStateProperty); }
            set { SetValue(AutoBackupStateProperty, value); }
        }

        public string AutoBackupStateString
        {
            get { return (string)GetValue(AutoBackupStateStringProperty); }
            set { SetValue(AutoBackupStateStringProperty, value); }
        }

        public string AutoBackupNextRunTime
        {
            get { return (string)GetValue(AutoBackupNextRunTimeProperty); }
            set { SetValue(AutoBackupNextRunTimeProperty, value); }
        }

        public Microsoft.Win32.TaskScheduler.TaskState AutoUpdateState
        {
            get { return (Microsoft.Win32.TaskScheduler.TaskState)GetValue(AutoUpdateStateProperty); }
            set { SetValue(AutoUpdateStateProperty, value); }
        }

        public string AutoUpdateStateString
        {
            get { return (string)GetValue(AutoUpdateStateStringProperty); }
            set { SetValue(AutoUpdateStateStringProperty, value); }
        }

        public string AutoUpdateNextRunTime
        {
            get { return (string)GetValue(AutoUpdateNextRunTimeProperty); }
            set { SetValue(AutoUpdateNextRunTimeProperty, value); }
        }

        public DiscordBot.Enums.BotState DiscordBotState
        {
            get { return (DiscordBot.Enums.BotState)GetValue(DiscordBotStateProperty); }
            set { SetValue(DiscordBotStateProperty, value); }
        }

        public string DiscordBotStateString
        {
            get { return (string)GetValue(DiscordBotStateStringProperty); }
            set { SetValue(DiscordBotStateStringProperty, value); }
        }

        public bool IsAdministrator
        {
            get;
            set;
        }

        public bool IsIpValid
        {
            get { return (bool)GetValue(IsIpValidProperty); }
            set { SetValue(IsIpValidProperty, value); }
        }

        public Version LatestServerManagerVersion
        {
            get { return (Version)GetValue(LatestServerManagerVersionProperty); }
            set { SetValue(LatestServerManagerVersionProperty, value); }
        }

        public bool NewServerManagerAvailable
        {
            get { return (bool)GetValue(NewServerManagerAvailableProperty); }
            set { SetValue(NewServerManagerAvailableProperty, value); }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //
            // Kick off the initialization.
            //
            TaskUtils.RunOnUIThreadAsync(() =>
                {
                    // We need to load the set of existing servers, or create a blank one if we don't have any...
                    foreach (var profile in Directory.EnumerateFiles(Config.Default.ConfigDirectory, "*" + Config.Default.ProfileExtension))
                    {
                        try
                        {
                            ServerManager.Instance.AddFromPath(profile);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format(_globalizer.GetResourceString("MainWindow_ProfileLoad_FailedLabel"), profile, ex.Message, ex.StackTrace), _globalizer.GetResourceString("MainWindow_ProfileLoad_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }

                    ServerManager.Instance.SortServers();
                    ServerManager.Instance.CheckProfiles();

                    Tabs.SelectedIndex = 0;
                }).DoNotWait();

            this.versionChecker.PostAction(CheckForUpdates).DoNotWait();
            this.scheduledTaskChecker.PostAction(CheckForScheduledTasks).DoNotWait();
            this.discordBotChecker.PostAction(CheckForDiscordBot).DoNotWait();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                Config.Default.MainWindow_Left = Math.Max(0D, this.Left);
                Config.Default.MainWindow_Top = Math.Max(0D, this.Top);
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                Config.Default.MainWindow_Height = e.NewSize.Height;
                Config.Default.MainWindow_Width = e.NewSize.Width;
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (Config.Default.MainWindow_MinimizeToTray && this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (sender is Window window)
                window.Closed -= Window_Closed;

            this.Activate();
        }

        protected override void OnClosed(EventArgs e)
        {
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent -= ResourceDictionaryChangedEvent;

            base.OnClosed(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DiscordBotHelper.HasRunningCommands)
            {
                var result = MessageBox.Show(_globalizer.GetResourceString("MainWindow_DiscordBot_RunningCommandsLabel"), _globalizer.GetResourceString("MainWindow_DiscordBot_RunningCommandsTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing(e);
            RCONWindow.CloseAllWindows();
            PlayerListWindow.CloseAllWindows();
            ServerMonitorWindow.CloseAllWindows();
            this.versionChecker.DisposeAsync().DoNotWait();
        }

        private void ResourceDictionaryChangedEvent(object source, ResourceDictionaryChangedEventArgs e)
        {
            if (IsAdministrator)
                this.Title = _globalizer.GetResourceString("MainWindow_TitleWithAdmin");
            else
                this.Title = _globalizer.GetResourceString("MainWindow_Title");

            this.versionChecker.PostAction(CheckForUpdates).DoNotWait();
            this.scheduledTaskChecker.PostAction(CheckForScheduledTasks).DoNotWait();
            this.discordBotChecker.PostAction(CheckForDiscordBot).DoNotWait();
        }

        private void PatchNotes_Click(object sender, RoutedEventArgs e)
        {
            var url = string.Empty;
            if (AppInstance.BetaVersion)
                url = Config.Default.ServerManagerVersionBetaFeedUrl;
            else
                url = Config.Default.ServerManagerVersionFeedUrl;

            if (!string.IsNullOrWhiteSpace(url))
            {
                var window = new VersionFeedWindow(url);
                window.Closed += Window_Closed;
                window.Owner = this;
                window.ShowDialog();
            }
            else
            {
                if (AppInstance.BetaVersion)
                    url = Config.Default.LatestASMBetaPatchNotesUrl;
                else
                    url = Config.Default.LatestASMPatchNotesUrl;

                if (string.IsNullOrWhiteSpace(url))
                    return;

                Process.Start(url);
            }
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Config.Default.DonationUrl))
                return;

            var result = MessageBox.Show(_globalizer.GetResourceString("MainWindow_Donate_Label"), _globalizer.GetResourceString("MainWindow_Donate_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Process.Start(Config.Default.DonationUrl);
            }
        }

        private void Help_Click(object sender, RoutedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(Config.Default.HelpUrl))
                return;

            Process.Start(Config.Default.HelpUrl);
        }

        private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            var logFolder = App.GetLogFolder();
            if (!Directory.Exists(logFolder))
                logFolder = Config.Default.DataDir;
            Process.Start("explorer.exe", logFolder);
        }

        private void RCON_Click(object sender, RoutedEventArgs e)
        {
            var window = new OpenRCONWindow();
            window.Closed += Window_Closed;
            window.Owner = this;
            window.ShowDialog();
        }

        private async void RefreshPublicIP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.DiscoverMachinePublicIPAsync(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Refresh Public IP Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs args)
        {
            var window = new SettingsWindow();
            window.Closed += Window_Closed;
            window.Owner = this;
            window.ShowDialog();
        }

        private void GameData_Click(object sender, RoutedEventArgs e)
        {
            var window = new GameDataWindow();
            window.Closed += Window_Closed;
            window.Owner = this;
            window.ShowDialog();
        }

        private void Plugins_Click(object sender, RoutedEventArgs e)
        {
            var window = new PluginsWindow();
            window.Closed += Window_Closed;
            window.Owner = this;
            window.ShowDialog();
        }

        private void ServerMonitor_Click(object sender, RoutedEventArgs e)
        {
            var window = ServerMonitorWindow.GetWindow(ServerManager);
            window.Closed += Window_Closed;
            window.Show();
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
            window.Focus();
        }

        private async void SteamCMD_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(_globalizer.GetResourceString("MainWindow_SteamCmd_Label"), _globalizer.GetResourceString("MainWindow_SteamCmd_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ProgressWindow window = null;

                try
                {
                    var updater = new SteamCmdUpdater();
                    var cancelSource = new CancellationTokenSource();

                    window = new ProgressWindow(_globalizer.GetResourceString("Progress_ReinstallSteamCmd_WindowTitle"));
                    window.Closed += Window_Closed;
                    window.Owner = this;
                    window.Show();

                    await Task.Delay(1000);
                    await updater.ReinstallSteamCmdAsync(Config.Default.DataDir, new Progress<SteamCmdUpdater.Update>(u =>
                    {
                        var resourceString = string.IsNullOrWhiteSpace(u.StatusKey) ? null : _globalizer.GetResourceString(u.StatusKey);
                        var message = resourceString != null ? $"{SteamCmdUpdater.OUTPUT_PREFIX} {resourceString}" : u.StatusKey;
                        window?.AddMessage(message);

                        if (u.FailureText != null)
                        {
                            message = string.Format(_globalizer.GetResourceString("MainWindow_SteamCmd_FailedLabel"), u.FailureText);
                            window?.AddMessage(message);
                            MessageBox.Show(message, _globalizer.GetResourceString("MainWindow_SteamCmd_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }), cancelSource.Token);
                }
                catch (Exception ex)
                {
                    var message = string.Format(_globalizer.GetResourceString("MainWindow_SteamCmd_FailedLabel"), ex.Message);
                    window?.AddMessage(message);
                    MessageBox.Show(message, _globalizer.GetResourceString("MainWindow_SteamCmd_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (window != null)
                        window.CloseWindow();
                }
            }
        }

        private async void Upgrade_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(String.Format(_globalizer.GetResourceString("MainWindow_Upgrade_Label"), this.LatestServerManagerVersion), _globalizer.GetResourceString("MainWindow_Upgrade_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(result == MessageBoxResult.Yes)
            {
                try
                {
                    OverlayMessage.Content = _globalizer.GetResourceString("MainWindow_OverlayMessage_UpgradeLabel");
                    OverlayGrid.Visibility = Visibility.Visible;

                    await Task.Delay(500);

                    var process = Process.GetCurrentProcess();
                    if (process == null || process.HasExited)
                        throw new Exception("Application process could not be found or does not exist.");

                    var assemblyLocation = Assembly.GetEntryAssembly().Location;
                    var updaterFile = Path.Combine(Path.GetDirectoryName(assemblyLocation), Config.Default.UpdaterFile);
                    var newUpdaterFile = Path.Combine(Path.GetDirectoryName(assemblyLocation), $"New{Config.Default.UpdaterFile}");

                    // check if there is a new version of the updater file
                    if (File.Exists(newUpdaterFile))
                    {
                        // file exists, overwrite the file, so that we use the new updater instead
                        try
                        {
                            File.Copy(newUpdaterFile, updaterFile, true);
                            await Task.Delay(1000);
                            File.Delete(newUpdaterFile);
                        }
                        catch (Exception ex)
                        {
                            // if error, then do nothing
                            Logger.Debug($"An error occurred trying to update the server manager updater. {ex.Message}");
                        }
                    }

                    if (!File.Exists(updaterFile))
                        throw new FileNotFoundException("The updater application could not be found or does not exist.");

                    var arguments = new string[]
                        {
                            process.Id.ToString().AsQuoted(),
                            App.Instance.BetaVersion ? Config.Default.LatestASMBetaDownloadUrl.AsQuoted() : Config.Default.LatestASMDownloadUrl.AsQuoted(),
                            Config.Default.UpdaterPrefix.AsQuoted(),
                        };

                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = updaterFile.AsQuoted(),
                        Arguments = string.Join(" ", arguments),
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    };

                    var updaterProcess = Process.Start(info);
                    if (updaterProcess == null)
                        throw new Exception("Could not restart application.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format(_globalizer.GetResourceString("MainWindow_Upgrade_FailedLabel"), ex.Message, ex.StackTrace), _globalizer.GetResourceString("MainWindow_Upgrade_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    OverlayGrid.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Config.Default.DiscordUrl))
                return;

            Process.Start(Config.Default.DiscordUrl);
        }

        public void Servers_AddNew(object sender, NewItemRequestedEventArgs e)
        {
            var index = this.ServerManager.AddNew();
            ((EO.Wpf.TabControl)e.Source).SelectedIndex = index;
        }

        public void Servers_Remove(object sender, TabItemCloseEventArgs args)
        {
            args.Canceled = true;
            var server = ServerManager.Instance.Servers[args.ItemIndex];
            var result = MessageBox.Show(_globalizer.GetResourceString("MainWindow_ProfileDelete_Label"), String.Format(_globalizer.GetResourceString("MainWindow_ProfileDelete_Title"), server.Profile.ProfileName), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if(result == MessageBoxResult.Yes)
            {
                ServerManager.Instance.Remove(server, deleteProfile: true);
                args.Canceled = false;
            }
        }

        private void AutoBackupTaskRun_Click(object sender, RoutedEventArgs e)
        {
            var taskKey = TaskSchedulerUtils.ComputeKey(Config.Default.DataDir);

            try
            {
                TaskSchedulerUtils.RunAutoBackup(taskKey, null);
            }
            catch (Exception)
            {
                // Ignore.
            }
        }

        private void AutoBackupTaskState_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdministrator)
            {
                MessageBox.Show(_globalizer.GetResourceString("MainWindow_TaskAdminErrorLabel"), _globalizer.GetResourceString("MainWindow_TaskAdminErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var taskKey = TaskSchedulerUtils.ComputeKey(Config.Default.DataDir);

            try
            {
                TaskSchedulerUtils.SetAutoBackupState(taskKey, null, null);
            }
            catch (Exception)
            {
                // Ignore.
            }
        }

        private void AutoUpdateTaskRun_Click(object sender, RoutedEventArgs e)
        {
            var taskKey = TaskSchedulerUtils.ComputeKey(Config.Default.DataDir);

            try
            {
                TaskSchedulerUtils.RunAutoUpdate(taskKey, null);
            }
            catch (Exception)
            {
                // Ignore.
            }
        }

        private void AutoUpdateTaskState_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdministrator)
            {
                MessageBox.Show(_globalizer.GetResourceString("MainWindow_TaskAdminErrorLabel"), _globalizer.GetResourceString("MainWindow_TaskAdminErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var taskKey = TaskSchedulerUtils.ComputeKey(Config.Default.DataDir);

            try
            {
                TaskSchedulerUtils.SetAutoUpdateState(taskKey, null, null);
            }
            catch (Exception)
            {
                // Ignore.
            }
        }

        private async void DiscordBotTaskState_Click(object sender, RoutedEventArgs e)
        {
            if (discordBotStateClicked)
                return;
            discordBotStateClicked = true;

            try
            {
                switch (DiscordBotState)
                {
                    case DiscordBot.Enums.BotState.Stopped:
                        AppInstance.StartDiscordBot();
                        break;
                    case DiscordBot.Enums.BotState.Running:
                        AppInstance.StopDiscordBot();
                        break;
                }

                await Task.Delay(5000);
            }
            catch (Exception)
            {
                // Ignore.
            }
            finally
            {
                discordBotStateClicked = false;
            }
        }

        public ICommand ShowWindowCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (parameter) =>
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                        this.Activate();
                    },
                    canExecute: (parameter) => 
                    {
                        return true;
                    }
                );
            }
        }

        public ICommand StatusButtonCommand
        {
            get
            {
                return new RelayCommand<Server>(
                    execute: (server) =>
                    {
                        Debug.WriteLine($"{server.Profile.ProfileName}: {server.Runtime.Status}");
                        if (!Config.Default.ServerStatus_EnableActions)
                            return;

                        switch (server.Runtime.Status)
                        {
                            case ServerStatus.Stopped:
                                if (Config.Default.ServerStatus_ShowActionConfirmation && MessageBox.Show(_globalizer.GetResourceString("MainWindow_ServerStatus_StartServerActionLabel"), _globalizer.GetResourceString("MainWindow_ServerStatus_StartServerActionTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                                    return;
                                StartServerAsync(server).DoNotWait();
                                break;

                            case ServerStatus.Running:
                                if (Config.Default.ServerStatus_ShowActionConfirmation && MessageBox.Show(_globalizer.GetResourceString("MainWindow_ServerStatus_ShutdownServerActionLabel"), _globalizer.GetResourceString("MainWindow_ServerStatus_ShutdownServerActionTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                                    return;
                                ShutdownServerAsync(server).DoNotWait();
                                break;
                        }
                    },
                    canExecute: (server) => server != null && server.Profile != null && server.Runtime != null
                );
            }
        }

        private async Task CheckForUpdates()
        {
            string url = App.Instance.BetaVersion ? Config.Default.LatestASMBetaVersionUrl : Config.Default.LatestASMVersionUrl;
            var newVersion = await NetworkUtils.GetLatestServerManagerVersion(url);

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                try
                {
                    var appVersion = new Version();
                    Version.TryParse(App.Instance.Version, out appVersion);

                    this.LatestServerManagerVersion = newVersion;
                    this.NewServerManagerAvailable = appVersion < newVersion;

                    Logger.Info($"{nameof(CheckForUpdates)} performed");
                }
                catch (Exception)
                {
                    // Ignore.
                }
            }).DoNotWait();

            await Task.Delay(Config.Default.UpdateCheckTime * 60 * 1000);
            this.versionChecker.PostAction(CheckForUpdates).DoNotWait();
        }

        private async Task CheckForScheduledTasks()
        {
            var taskKey = TaskSchedulerUtils.ComputeKey(Config.Default.DataDir);

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                try
                {
                    var backupState = TaskSchedulerUtils.TaskStateAutoBackup(taskKey, null, out DateTime backupnextRunTime);
                    var updateState = TaskSchedulerUtils.TaskStateAutoUpdate(taskKey, null, out DateTime updatenextRunTime);

                    this.AutoBackupState = backupState;
                    this.AutoUpdateState = updateState;

                    this.AutoBackupStateString = GetTaskSchedulerStateString(AutoBackupState);
                    this.AutoUpdateStateString = GetTaskSchedulerStateString(AutoUpdateState);

                    this.AutoBackupNextRunTime = backupnextRunTime == DateTime.MinValue ? string.Empty : $"{_globalizer.GetResourceString("MainWindow_TaskRunTimeLabel")} {backupnextRunTime:G}";
                    this.AutoUpdateNextRunTime = updatenextRunTime == DateTime.MinValue ? string.Empty : $"{_globalizer.GetResourceString("MainWindow_TaskRunTimeLabel")} {updatenextRunTime:G}";

                    Logger.Info($"{nameof(CheckForScheduledTasks)} performed");
                }
                catch (Exception)
                {
                    // Ignore.
                }
            }).DoNotWait();

            await Task.Delay(Config.Default.ScheduledTasksCheckTime * 1 * 1000);
            this.scheduledTaskChecker.PostAction(CheckForScheduledTasks).DoNotWait();
        }

        private async Task CheckForDiscordBot()
        {
            TaskUtils.RunOnUIThreadAsync(() =>
            {
                try
                {
                    var botState = DiscordBot.Enums.BotState.Unknown;
                    if (AppInstance.DiscordBotStarted)
                    {
                        botState = DiscordBot.Enums.BotState.Running;
                    }
                    else
                    {
                        if (Config.DiscordBotEnabled)
                        {
                            botState = DiscordBot.Enums.BotState.Stopped;
                        }
                        else
                        {
                            botState = DiscordBot.Enums.BotState.Disabled;
                        }
                    }

                    this.DiscordBotState = botState;

                    this.DiscordBotStateString = GetDiscordBotStateString(botState);

                    Logger.Info($"{nameof(CheckForDiscordBot)} performed");
                }
                catch (Exception)
                {
                    // Ignore.
                }
            }).DoNotWait();

            await Task.Delay(Config.Default.DiscordBotStatusCheckTime * 1 * 1000);
            this.discordBotChecker.PostAction(CheckForDiscordBot).DoNotWait();
        }

        private string GetTaskSchedulerStateString(Microsoft.Win32.TaskScheduler.TaskState taskState)
        {
            switch (taskState)
            {
                case Microsoft.Win32.TaskScheduler.TaskState.Disabled:
                    return _globalizer.GetResourceString("MainWindow_TaskStateDisabledLabel");
                case Microsoft.Win32.TaskScheduler.TaskState.Queued:
                    return _globalizer.GetResourceString("MainWindow_TaskStateQueuedLabel");
                case Microsoft.Win32.TaskScheduler.TaskState.Ready:
                    return _globalizer.GetResourceString("MainWindow_TaskStateReadyLabel");
                case Microsoft.Win32.TaskScheduler.TaskState.Running:
                    return _globalizer.GetResourceString("MainWindow_TaskStateRunningLabel");
                case Microsoft.Win32.TaskScheduler.TaskState.Unknown:
                    return _globalizer.GetResourceString("MainWindow_TaskStateUnknownLabel");
                default:
                    return _globalizer.GetResourceString("MainWindow_TaskStateUnknownLabel");
            }
        }

        private string GetDiscordBotStateString(DiscordBot.Enums.BotState botState)
        {
            switch (botState)
            {
                case DiscordBot.Enums.BotState.Disabled:
                    return _globalizer.GetResourceString("MainWindow_TaskStateDisabledLabel");
                case DiscordBot.Enums.BotState.Running:
                    return _globalizer.GetResourceString("MainWindow_TaskStateRunningLabel");
                case DiscordBot.Enums.BotState.Stopped:
                    return _globalizer.GetResourceString("MainWindow_TaskStateStoppedLabel");
                default:
                    return _globalizer.GetResourceString("MainWindow_TaskStateUnknownLabel");
            }
        }

        private async Task StartServerAsync(Server server)
        {
            if (server == null || server.Profile == null || server.Runtime == null || server.Runtime.Status != ServerStatus.Stopped)
                return;

            Mutex mutex = null;
            bool createdNew = false;

            try
            {
                // try to establish a mutex for the profile.
                mutex = new Mutex(true, ServerApp.GetMutexName(server.Profile.InstallDirectory), out createdNew);

                // check if the mutex was established
                if (createdNew)
                {
                    server.Profile.Save(false, false, null);

                    if (!server.Profile.Validate(false, out string validateMessage))
                    {
                        var outputMessage = _globalizer.GetResourceString("ProfileValidation_WarningLabel").Replace("{validateMessage}", validateMessage);
                        if (MessageBox.Show(outputMessage, _globalizer.GetResourceString("ProfileValidation_Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            return;
                    }

                    await server.StartAsync();

                    var startupMessage = Config.Default.Alert_ServerStartedMessage;
                    if (Config.Default.Alert_ServerStartedMessageIncludeIPandPort && !string.IsNullOrWhiteSpace(Config.Default.Alert_ServerStartedMessageIPandPort))
                    {
                        var ipAndPortMessage = Config.Default.Alert_ServerStartedMessageIPandPort
                            .Replace("{ipaddress}", Config.Default.MachinePublicIP)
                            .Replace("{port}", server.Profile.QueryPort.ToString());
                        startupMessage += $" {ipAndPortMessage}";
                    }
                    PluginHelper.Instance.ProcessAlert(AlertType.Startup, server.Profile.ProfileName, startupMessage);

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
                }
            }
        }

        private async Task ShutdownServerAsync(Server server)
        {
            if (server == null || server.Profile == null || server.Runtime == null || server.Runtime.Status != ServerStatus.Running)
                return;

            try
            {
                var shutdownWindow = ShutdownWindow.OpenShutdownWindow(server);
                if (shutdownWindow == null)
                {
                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ShutdownServer_AlreadyOpenLabel"), _globalizer.GetResourceString("ServerSettings_ShutdownServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                server.Runtime.UpdateServerStatus(ServerStatus.Stopping, AvailabilityStatus.Unavailable, false);

                shutdownWindow.CloseShutdownWindowWhenFinished = true;
                shutdownWindow.Owner = this;
                shutdownWindow.Closed += Window_Closed;
                shutdownWindow.Show();
                await shutdownWindow.StartShutdownAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ShutdownServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
