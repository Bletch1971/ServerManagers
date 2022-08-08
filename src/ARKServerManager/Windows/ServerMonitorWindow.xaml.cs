using IWshRuntimeLibrary;
using NLog;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using ServerManagerTool.Plugin.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Windows
{
    /// <summary>
    /// Interaction logic for ServerMonitorWindow.xaml
    /// </summary>
    public partial class ServerMonitorWindow : Window
    {
        public class ServerMonitorOutput_Critical : Run
        {
            public ServerMonitorOutput_Critical(string value)
                : base(value)
            {
                Foreground = Brushes.Red;
                FontWeight = FontWeights.Bold;
                FontSize = FontSize + 2;
            }
        }

        public class ServerMonitorOutput_Error : Run
        {
            public ServerMonitorOutput_Error(string value)
                : base(value)
            {
                Foreground = Brushes.Red;
            }
        }

        public class ServerMonitorOutput_Success : Run
        {
            public ServerMonitorOutput_Success(string value)
                : base(value)
            {
                Foreground = Brushes.Green;
            }
        }

        public class ServerMonitorOutput_Warning : Run
        {
            public ServerMonitorOutput_Warning(string value)
                : base(value)
            {
                Foreground = Brushes.Orange;
                FontWeight = FontWeights.Bold;
            }
        }

        private const int DELAY_PROCESSCOMPLETE = 5000; // 5 seconds

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly List<ServerMonitorWindow> Windows = new List<ServerMonitorWindow>();

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private CancellationTokenSource _upgradeCancellationSource = null;
        private ActionQueue _versionChecker;
        private ActionQueue _canExecuteChecker;
        private readonly Dictionary<string, CommandType> _currentProfileCommands = new Dictionary<string, CommandType>();

        private bool HasRunningCommands => _currentProfileCommands.Count > 0;

        public static readonly DependencyProperty ServerManagerProperty = DependencyProperty.Register(nameof(ServerManager), typeof(ServerManager), typeof(ServerMonitorWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty LatestServerManagerVersionProperty = DependencyProperty.Register(nameof(LatestServerManagerVersion), typeof(Version), typeof(ServerMonitorWindow), new PropertyMetadata(new Version()));
        public static readonly DependencyProperty ShowUpdateButtonProperty = DependencyProperty.Register(nameof(ShowUpdateButton), typeof(bool), typeof(ServerMonitorWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty IsStandAloneWindowProperty = DependencyProperty.Register(nameof(IsStandAloneWindow), typeof(bool), typeof(ServerMonitorWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty CancellationTokenSourceProperty = DependencyProperty.Register(nameof(CancellationTokenSource), typeof(CancellationTokenSource), typeof(ServerMonitorWindow));
        public static readonly DependencyProperty ProcessServersSequentiallyProperty = DependencyProperty.Register(nameof(ProcessServersSequentially), typeof(bool), typeof(ServerMonitorWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty SequentialProcessDelayProperty = DependencyProperty.Register(nameof(SequentialProcessDelay), typeof(int), typeof(ServerMonitorWindow), new PropertyMetadata(10));
        public static readonly DependencyProperty ShutdownReasonProperty = DependencyProperty.Register(nameof(ShutdownReason), typeof(string), typeof(ServerMonitorWindow), new PropertyMetadata(null));

        public ServerMonitorWindow() : this(null)
        {
        }

        public ServerMonitorWindow(ServerManager serverManager)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.ServerManager = serverManager;
            this.IsStandAloneWindow = serverManager == null;
            this.DataContext = this;

            SetWindowTitle();

            this.Left = Config.Default.ServerMonitorWindow_Left;
            this.Top = Config.Default.ServerMonitorWindow_Top;
            this.Height = Config.Default.ServerMonitorWindow_Height;
            this.Width = Config.Default.ServerMonitorWindow_Width;
            this.WindowState = Config.Default.ServerMonitorWindow_WindowState;

            // hook into the language change event
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent += ResourceDictionaryChangedEvent;

            Windows.Add(this);
        }

        public ServerManager ServerManager
        {
            get { return (ServerManager)GetValue(ServerManagerProperty); }
            set { SetValue(ServerManagerProperty, value); }
        }

        public Version LatestServerManagerVersion
        {
            get { return (Version)GetValue(LatestServerManagerVersionProperty); }
            set { SetValue(LatestServerManagerVersionProperty, value); }
        }

        public bool ShowUpdateButton
        {
            get { return (bool)GetValue(ShowUpdateButtonProperty); }
            set { SetValue(ShowUpdateButtonProperty, value); }
        }

        public bool IsStandAloneWindow
        {
            get { return (bool)GetValue(IsStandAloneWindowProperty); }
            set { SetValue(IsStandAloneWindowProperty, value); }
        }

        public CancellationTokenSource CancellationTokenSource
        {
            get { return (CancellationTokenSource)GetValue(CancellationTokenSourceProperty); }
            set { SetValue(CancellationTokenSourceProperty, value); }
        }

        public bool ProcessServersSequentially
        {
            get { return (bool)GetValue(ProcessServersSequentiallyProperty); }
            set { SetValue(ProcessServersSequentiallyProperty, value); }
        }

        public int SequentialProcessDelay
        {
            get { return (int)GetValue(SequentialProcessDelayProperty); }
            set { SetValue(SequentialProcessDelayProperty, value); }
        }

        public string ShutdownReason
        {
            get { return (string)GetValue(ShutdownReasonProperty); }
            set { SetValue(ShutdownReasonProperty, value); }
        }

        private void ServerMonitorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (ServerManager == null)
            {
                ServerManager = ServerManager.Instance;

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

                }).DoNotWait();
            }

            if (IsStandAloneWindow)
            {
                _versionChecker = new ActionQueue();
                _versionChecker.PostAction(CheckForUpdates).DoNotWait();
            }

            _canExecuteChecker = new ActionQueue();
            _canExecuteChecker.PostAction(RaiseCanExecuteChanged).DoNotWait();
        }

        private void ServerMonitorWindow_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                Config.Default.ServerMonitorWindow_Left = Math.Max(0D, this.Left);
                Config.Default.ServerMonitorWindow_Top = Math.Max(0D, this.Top);
            }
        }

        private void ServerMonitorWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState != WindowState.Minimized)
            {
                Config.Default.ServerMonitorWindow_Height = e.NewSize.Height;
                Config.Default.ServerMonitorWindow_Width = e.NewSize.Width;
            }
        }

        private void ServerMonitorWindow_StateChanged(object sender, EventArgs e)
        {
            if (IsStandAloneWindow && Config.Default.MainWindow_MinimizeToTray && this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Activate();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (HasRunningCommands)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_RunningProcesses_ConfirmLabel"), _globalizer.GetResourceString("ServerMonitor_RunningProcesses_ConfirmTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                
                e.Cancel = true;
                return;
            }

            if (this.OwnedWindows.OfType<ProgressWindow>().Any())
            {
                if (MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_CloseWindow_ConfirmLabel"), _globalizer.GetResourceString("ServerMonitor_CloseWindow_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            Windows.Remove(this);

            _versionChecker?.DisposeAsync().DoNotWait();
            _canExecuteChecker?.DisposeAsync().DoNotWait();

            base.OnClosing(e);
        }

        private void ResourceDictionaryChangedEvent(object source, ResourceDictionaryChangedEventArgs e)
        {
            SetWindowTitle();
        }

        private async void BackupServer_Click(object sender, RoutedEventArgs e)
        {
            var server = ((Server)((Button)e.Source).DataContext);
            if (server == null)
                return;

            try
            {
                var app = new ServerApp(true)
                {
                    DeleteOldBackupFiles = !Config.Default.AutoBackup_EnableBackup,
                    SendEmails = false,
                    OutputLogs = false,
                    ServerProcess = ServerProcessType.Backup,
                };

                var profile = ServerProfileSnapshot.Create(server.Profile);

                var exitCode = await Task.Run(() => app.PerformProfileBackup(profile, CancellationToken.None));
                if (exitCode != ServerApp.EXITCODE_NORMALEXIT && exitCode != ServerApp.EXITCODE_CANCELLED)
                {
                    throw new ApplicationException($"An error occured during the backup process - ExitCode: {exitCode}");
                }

                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_BackupServer_SuccessfulLabel"), _globalizer.GetResourceString("ServerSettings_BackupServer_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_BackupServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateShortcut_Click(object sender, RoutedEventArgs e)
        {
            var invalidChars = Path.GetInvalidFileNameChars().ToList();

            var name = $"{_globalizer.GetResourceString("MainWindow_Title")} - {_globalizer.GetResourceString("ServerMonitor_Title")}";
            invalidChars.ForEach(c => name = name.Replace(c.ToString(), ""));

            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutLocation = Path.Combine(desktopPath, name + ".lnk");
            var applicationFile = Assembly.GetExecutingAssembly().Location;

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = name;
            shortcut.Arguments = "-sm";
            shortcut.IconLocation = applicationFile;
            shortcut.TargetPath = applicationFile;
            shortcut.WorkingDirectory = Path.GetDirectoryName(applicationFile);
            shortcut.Save();
        }

        private void OpenPlayerList_Click(object sender, RoutedEventArgs e)
        {
            var server = ((Server)((Button)e.Source).DataContext);
            if (server == null)
                return;

            Window window = null;

            if (server.Profile.RCONEnabled)
            {
                window = RCONWindow.GetRCONForServer(server);
            }
            else
            {
                window = PlayerListWindow.GetWindowForServer(server);
            }

            window.Closed += Window_Closed;
            window.Show();
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Focus();
        }

        private void OpenServerFolder_Click(object sender, RoutedEventArgs e)
        {
            var server = ((Server)((Button)e.Source).DataContext);
            if (server == null)
                return;

            Process.Start("explorer.exe", server.Profile.InstallDirectory);
        }

        private void PatchNotes_Click(object sender, RoutedEventArgs e)
        {
            var url = string.Empty;
            if (App.Instance.BetaVersion)
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
                if (App.Instance.BetaVersion)
                    url = Config.Default.LatestASMBetaPatchNotesUrl;
                else
                    url = Config.Default.LatestASMPatchNotesUrl;

                if (string.IsNullOrWhiteSpace(url))
                    return;

                Process.Start(url);
            }
        }

        private async void StartStopServer_Click(object sender, RoutedEventArgs e)
        {
            var server = ((Server)((Button)e.Source).DataContext);
            if (server == null)
                return;

            await StartStopServerAsync(server);
        }

        private async void UpdateServer_Click(object sender, RoutedEventArgs e)
        {
            var server = ((Server)((Button)e.Source).DataContext);
            if (server == null)
                return;

            switch (server.Runtime.Status)
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
                server.Profile.Save(false, false, null);
                await UpdateServerAsync(server, true, true, Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_UpdateServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpgradeApplication_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(String.Format(_globalizer.GetResourceString("MainWindow_Upgrade_Label"), this.LatestServerManagerVersion), _globalizer.GetResourceString("MainWindow_Upgrade_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
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
                    if (System.IO.File.Exists(newUpdaterFile))
                    {
                        // file exists, rename the file, so that we use the new updater instead
                        try
                        {
                            System.IO.File.Copy(newUpdaterFile, updaterFile, true);
                            await Task.Delay(1000);
                            System.IO.File.Delete(newUpdaterFile);
                        }
                        catch (Exception ex)
                        {
                            // if error, then do nothing
                            Logger.Debug($"An error occurred trying to update the server manager updater. {ex.Message}");
                        }
                    }

                    if (!System.IO.File.Exists(updaterFile))
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

        public void AddCriticalBlockContent(string message)
        {
            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var p = new Paragraph();

                p.Inlines.Add(new ServerMonitorOutput_Critical(message));

                ConsoleContent.Blocks.Add(p);
            }).DoNotWait();
        }

        public void AddErrorBlockContent(string message)
        {
            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var p = new Paragraph();

                p.Inlines.Add(new ServerMonitorOutput_Error(message));

                ConsoleContent.Blocks.Add(p);
            }).DoNotWait();
        }

        public void AddMessageBlockContent(string message)
        {
            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var p = new Paragraph();

                p.Inlines.Add(new ServerMonitorOutput_Success(message));

                ConsoleContent.Blocks.Add(p);
            }).DoNotWait();
        }

        public void AddWarningBlockContent(string message)
        {
            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var p = new Paragraph();

                p.Inlines.Add(new ServerMonitorOutput_Warning(message));

                ConsoleContent.Blocks.Add(p);
            }).DoNotWait();
        }

        public void ClearBlockContents()
        {
            ConsoleContent.Blocks.Clear();
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
                    this.ShowUpdateButton = appVersion < newVersion;

                    Logger.Info($"{nameof(CheckForUpdates)} performed.");
                }
                catch (Exception)
                {
                    // Ignore.
                }
            }).DoNotWait();

            await Task.Delay(Config.Default.UpdateCheckTime * 60 * 1000);
            _versionChecker?.PostAction(CheckForUpdates).DoNotWait();
        }

        public static void CloseAllWindows()
        {
            var windows = Windows.ToArray();
            foreach (var window in windows)
            {
                if (window.IsLoaded)
                    window.Close();
            }
            Windows.Clear();
        }

        public static ServerMonitorWindow GetWindow(ServerManager serverManager)
        {
            if (Windows.Count > 0)
                return Windows[0];

            return new ServerMonitorWindow(serverManager);
        }

        public async Task RaiseCanExecuteChanged()
        {
            await TaskUtils.RunOnUIThreadAsync(() => CommandManager.InvalidateRequerySuggested());
            await Task.Delay(5000);

            _canExecuteChecker?.PostAction(RaiseCanExecuteChanged).DoNotWait();
        }

        private void SetWindowTitle()
        {
            if (!string.IsNullOrWhiteSpace(App.Instance.Title))
            {
                this.Title = App.Instance.Title;
            }
            else
            {
                this.Title = _globalizer.GetResourceString("MainWindow_Title");
            }

            this.Title += $" - {_globalizer.GetResourceString("ServerMonitor_Title")}";

            if (IsStandAloneWindow)
            {
                this.Title += $" - {App.Instance.Version}";
            }
        }

        private async Task StartStopServerAsync(Server server)
        {
            if (server == null)
                return;

            var serverRuntime = server.Runtime;
            var serverProfile = server.Profile;
            var result = MessageBoxResult.None;

            switch (server.Runtime.Status)
            {
                case ServerStatus.Initializing:
                case ServerStatus.Running:
                    // check if the server is initialising.
                    if (serverRuntime.Status == ServerStatus.Initializing)
                    {
                        result = MessageBox.Show(_globalizer.GetResourceString("ServerSettings_StartServer_StartingLabel"), _globalizer.GetResourceString("ServerSettings_StartServer_StartingTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                            return;

                        try
                        {
                            PluginHelper.Instance.ProcessAlert(AlertType.Shutdown, serverProfile.ProfileName, Config.Default.Alert_ServerStopMessage);
                            await Task.Delay(2000);

                            await server.StopAsync();
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
                            var shutdownWindow = ShutdownWindow.OpenShutdownWindow(server);
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
                        mutex = new Mutex(true, ServerApp.GetMutexName(serverProfile.InstallDirectory), out createdNew);

                        // check if the mutex was established
                        if (createdNew)
                        {
                            serverProfile.Save(false, false, null);

                            if (Config.Default.ServerUpdate_OnServerStart)
                            {
                                if (!await UpdateServerAsync(server, false, true, Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer, true))
                                {
                                    if (MessageBox.Show(_globalizer.GetResourceString("ServerUpdate_WarningLabel"), _globalizer.GetResourceString("ServerUpdate_Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                        return;
                                }
                            }

                            if (!serverProfile.Validate(false, out string validateMessage))
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
                                    .Replace("{port}", serverProfile.QueryPort.ToString());
                                startupMessage += $" {ipAndPortMessage}";
                            }
                            PluginHelper.Instance.ProcessAlert(AlertType.Startup, serverProfile.ProfileName, startupMessage);

                            if (serverProfile.ForceRespawnDinos)
                                PluginHelper.Instance.ProcessAlert(AlertType.Startup, serverProfile.ProfileName, Config.Default.Alert_ForceRespawnDinos);

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

        private async Task<bool> UpdateServerAsync(Server server, bool establishLock, bool updateServer, bool updateMods, bool closeProgressWindow)
        {
            if (server == null)
                return false;

            if (_upgradeCancellationSource != null)
            {
                // display an error message and exit
                MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_UpgradeServer_FailedLabel"), _globalizer.GetResourceString("ServerMonitor_UpgradeServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            ProgressWindow window = null;
            Mutex mutex = null;
            bool createdNew = !establishLock;
            var serverProfile = server.Profile;

            try
            {
                if (establishLock)
                {
                    // try to establish a mutex for the profile.
                    mutex = new Mutex(true, ServerApp.GetMutexName(serverProfile.InstallDirectory), out createdNew);
                }

                // check if the mutex was established
                if (createdNew)
                {
                    _upgradeCancellationSource = new CancellationTokenSource();

                    window = new ProgressWindow(string.Format(_globalizer.GetResourceString("Progress_UpgradeServer_WindowTitle"), serverProfile.ProfileName))
                    {
                        Owner = Window.GetWindow(this)
                    };
                    window.Closed += Window_Closed;
                    window.Show();

                    await Task.Delay(1000);

                    var branch = BranchSnapshot.Create(serverProfile);
                    return await server.UpgradeAsync(_upgradeCancellationSource.Token, updateServer, branch, true, updateMods, (p, m, n) => { TaskUtils.RunOnUIThreadAsync(() => { window?.AddMessage(m, n); }).DoNotWait(); });
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
                _upgradeCancellationSource = null;

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

        public ICommand BackupServersCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: async (_) =>
                    {
                        var processServersSequentially = ProcessServersSequentially;
                        var sequentialProcessDelay = SequentialProcessDelay;

                        await BackupSelectedServersAsync(processServersSequentially, sequentialProcessDelay);
                    },
                    canExecute: (_) =>
                    {
                        return ServerManager?.Servers != null && ServerManager.Servers.Count > 0 && ServerManager.Servers.Any(s => s.Selected) && ServerManager.Servers.All(s => s.Runtime.Status != ServerStatus.Unknown)
                            && CancellationTokenSource == null;
                    }
                );
            }
        }

        public ICommand CancelServersCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        CancellationTokenSource.Cancel();

                        AddWarningBlockContent(_globalizer.GetResourceString("ServerMonitor_ProcessServer_CancelRequestedLabel"));
                    },
                    canExecute: (_) =>
                    {
                        return CancellationTokenSource != null && !CancellationTokenSource.IsCancellationRequested;
                    }
                );
            }
        }

        public ICommand RestartServersCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: async (_) =>
                    {
                        var processServersSequentially = ProcessServersSequentially;
                        var sequentialProcessDelay = SequentialProcessDelay;
                        var shutdownReason = ShutdownReason;

                        await StartSelectedServersAsync(restart: true, processServersSequentially, sequentialProcessDelay, shutdownReason);
                    },
                    canExecute: (_) =>
                    {
                        return ServerManager?.Servers != null && ServerManager.Servers.Count > 0 && ServerManager.Servers.Any(s => s.Selected) && ServerManager.Servers.All(s => s.Runtime.Status != ServerStatus.Unknown)
                            && CancellationTokenSource == null;
                    }
                );
            }
        }

        public ICommand ShutdownServersCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: async (_) =>
                    {
                        var processServersSequentially = ProcessServersSequentially;
                        var sequentialProcessDelay = SequentialProcessDelay;
                        var shutdownReason = ShutdownReason;

                        await StopSelectedServersAsync(shutdown: true, processServersSequentially, sequentialProcessDelay, shutdownReason);
                    },
                    canExecute: (_) =>
                    {
                        return ServerManager?.Servers != null && ServerManager.Servers.Count > 0 && ServerManager.Servers.Any(s => s.Selected) && ServerManager.Servers.All(s => s.Runtime.Status != ServerStatus.Unknown)
                            && CancellationTokenSource == null;
                    }
                );
            }
        }

        public ICommand StartServersCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: async (_) =>
                    {
                        var processServersSequentially = ProcessServersSequentially;
                        var sequentialProcessDelay = SequentialProcessDelay;

                        await StartSelectedServersAsync(restart: false, processServersSequentially, sequentialProcessDelay, shutdownReason: null);
                    },
                    canExecute: (_) =>
                    {
                        return ServerManager?.Servers != null && ServerManager.Servers.Count > 0 && ServerManager.Servers.Any(s => s.Selected) && ServerManager.Servers.All(s => s.Runtime.Status != ServerStatus.Unknown)
                            && CancellationTokenSource == null;
                    }
                );
            }
        }

        public ICommand StopServersCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: async (_) =>
                    {
                        var processServersSequentially = ProcessServersSequentially;
                        var sequentialProcessDelay = SequentialProcessDelay;
                        var shutdownReason = ShutdownReason;

                        await StopSelectedServersAsync(shutdown: false, processServersSequentially, sequentialProcessDelay, shutdownReason);
                    },
                    canExecute: (_) =>
                    {
                        return ServerManager?.Servers != null && ServerManager.Servers.Count > 0 && ServerManager.Servers.Any(s => s.Selected) && ServerManager.Servers.All(s => s.Runtime.Status != ServerStatus.Unknown)
                            && CancellationTokenSource == null;
                    }
                );
            }
        }

        public ICommand UpdateModsCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: async (_) =>
                    {
                        var processServersSequentially = ProcessServersSequentially;
                        var sequentialProcessDelay = SequentialProcessDelay;
                        var shutdownReason = ShutdownReason;

                        await UpdateSelectedServersAsync(updateModsOnly: true, processServersSequentially, sequentialProcessDelay, shutdownReason);
                    },
                    canExecute: (_) =>
                    {
                        return ServerManager?.Servers != null && ServerManager.Servers.Count > 0 && ServerManager.Servers.Any(s => s.Selected) && ServerManager.Servers.All(s => s.Runtime.Status != ServerStatus.Unknown)
                            && CancellationTokenSource == null;
                    }
                );
            }
        }

        public ICommand UpdateServersCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: async (_) =>
                    {
                        var processServersSequentially = ProcessServersSequentially;
                        var sequentialProcessDelay = SequentialProcessDelay;
                        var shutdownReason = ShutdownReason;

                        await UpdateSelectedServersAsync(updateModsOnly: false, processServersSequentially, sequentialProcessDelay, shutdownReason);
                    },
                    canExecute: (_) =>
                    {
                        return ServerManager?.Servers != null && ServerManager.Servers.Count > 0 && ServerManager.Servers.Any(s => s.Selected) && ServerManager.Servers.All(s => s.Runtime.Status != ServerStatus.Unknown)
                            && CancellationTokenSource == null;
                    }
                );
            }
        }

        #region Drag and Drop

        public static readonly DependencyProperty DraggedItemProperty = DependencyProperty.Register(nameof(DraggedItem), typeof(Server), typeof(ServerMonitorWindow), new PropertyMetadata(null));

        public Server DraggedItem
        {
            get { return (Server)GetValue(DraggedItemProperty); }
            set { SetValue(DraggedItemProperty, value); }
        }

        public bool IsDragging { get; set; }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ResetDragDrop();

            // check fi the column is a template column (no drag-n-drop for those column types)
            var cell = WindowUtils.TryFindFromPoint<DataGridCell>((UIElement)sender, e.GetPosition(ServersGrid));
            if (cell != null) return;

            // check if we have a valid row
            var row = WindowUtils.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(ServersGrid));
            if (row == null) return;

            // set flag that indicates we're capturing mouse movements
            IsDragging = true;
            DraggedItem = (Server)row.Item;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsDragging)
            {
                if (popup.IsOpen)
                    popup.IsOpen = false;
                return;
            }

            //get the target item
            var targetItem = (Server)ServersGrid.SelectedItem;

            if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
            {
                //get target index
                var targetIndex = ServerManager.Servers.IndexOf(targetItem);

                //move source at the target's location
                Move(DraggedItem, targetIndex);

                //select the dropped item
                ServersGrid.SelectedItem = DraggedItem;
            }

            //reset
            ResetDragDrop();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsDragging || e.LeftButton != MouseButtonState.Pressed)
            {
                if (popup.IsOpen)
                    popup.IsOpen = false;
                return;
            }

            // display the popup if it hasn't been opened yet
            if (!popup.IsOpen)
            {
                // switch to read-only mode
                ServersGrid.IsReadOnly = true;

                // make sure the popup is visible
                popup.IsOpen = true;
            }

            var popupSize = new Size(popup.ActualWidth, popup.ActualHeight);
            popup.PlacementRectangle = new Rect(e.GetPosition(this), popupSize);

            // make sure the row under the grid is being selected
            var position = e.GetPosition(ServersGrid);
            var row = WindowUtils.TryFindFromPoint<DataGridRow>(ServersGrid, position);
            if (row != null) ServersGrid.SelectedItem = row.Item;
        }

        public void Move(Server draggedItem, int newIndex)
        {
            if (draggedItem == null)
                return;

            var index = ServerManager.Servers.IndexOf(draggedItem);
            if (index < 0)
                return;

            ServerManager.Servers.Move(index, newIndex);
            UpdateSequence();
        }

        private void ResetDragDrop()
        {
            IsDragging = false;
            popup.IsOpen = false;
            ServersGrid.IsReadOnly = false;
        }

        public void UpdateSequence()
        {
            foreach (var server in ServerManager.Servers)
            {
                server.Profile.Sequence = ServerManager.Servers.IndexOf(server) + 1;
            }
        }

        #endregion

        private void SelectAllServers_Click(object sender, RoutedEventArgs e)
        {
            foreach (var server in ServerManager.Servers)
            {
                server.Selected = true;
            }
        }

        private void UnselectAllServers_Click(object sender, RoutedEventArgs e)
        {
            foreach (var server in ServerManager.Servers)
            {
                server.Selected = false;
            }
        }

        private async Task BackupSelectedServersAsync(bool processServersSequentially, int sequentialProcessDelay)
        {
            if (CancellationTokenSource != null)
                return;

            var serverList = ServerManager.Servers.Where(s => s.Selected);
            if (serverList.IsEmpty())
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_NoServersSelected_ErrorLabel"), _globalizer.GetResourceString("ServerMonitor_NoServersSelected_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_BackupServers_ConfirmLabel"), _globalizer.GetResourceString("ServerMonitor_BackupServers_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            ClearBlockContents();

            var profileList = new List<ServerProfileSnapshot>();

            foreach (var server in serverList)
            {
                // check if another command is being run against the profile
                if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                {
                    AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ServerName));
                    continue;
                }

                switch (server.Runtime.Status)
                {
                    case ServerStatus.Initializing:
                    case ServerStatus.Stopping:
                    case ServerStatus.Uninstalled:
                    case ServerStatus.Unknown:
                    case ServerStatus.Updating:
                        AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                        continue;
                }

                _currentProfileCommands.Add(server.Profile.ProfileID, CommandType.Backup);
                profileList.Add(ServerProfileSnapshot.Create(server.Profile));
            }

            CancellationTokenSource = new CancellationTokenSource();
            var token = CancellationTokenSource.Token;
            var tasks = new List<Task>();

            foreach (var profile in profileList)
            {
                var app = new ServerApp(true)
                {
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ServerProcess = ServerProcessType.Backup,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.FirstOrDefault(s => string.Equals(profile.ProfileId, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase));
                            if (server != null)
                            {
                                server.Runtime.UpdateServerStatus(serverStatus, serverStatus != ServerStatus.Unknown);
                            }
                        }).Wait(token);
                    }
                };

                var task = new Task(() =>
                {
                    AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_BackupRequested"), profile.ServerName));

                    app.PerformProfileBackup(profile, token);

                    Task.Delay(DELAY_PROCESSCOMPLETE).Wait();

                    AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_CommandComplete"), profile.ServerName));
                    _currentProfileCommands.Remove(profile.ProfileId);
                }, token);

                tasks.Add(task);
            }

            try
            {
                if (processServersSequentially)
                {
                    foreach (var task in tasks)
                    {
                        token.ThrowIfCancellationRequested();

                        task.Start();
                        await task;

                        await Task.Delay(sequentialProcessDelay * 1000, token);
                    }
                }
                else
                {
                    foreach (var task in tasks)
                    {
                        token.ThrowIfCancellationRequested();

                        task.Start();
                    }
                    await Task.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException)
            {
                AddCriticalBlockContent(_globalizer.GetResourceString("ServerMonitor_ProcessServer_CancelledLabel"));
            }
            catch (Exception)
            {
            }
            finally
            {
                _currentProfileCommands.Clear();
                CancellationTokenSource?.Dispose();
                CancellationTokenSource = null;
            }
        }

        private async Task StartSelectedServersAsync(bool restart, bool processServersSequentially, int sequentialProcessDelay, string shutdownReason)
        {
            if (CancellationTokenSource != null)
                return;

            var serverList = ServerManager.Servers.Where(s => s.Selected);
            if (serverList.IsEmpty())
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_NoServersSelected_ErrorLabel"), _globalizer.GetResourceString("ServerMonitor_NoServersSelected_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_StartServers_ConfirmLabel"), _globalizer.GetResourceString("ServerMonitor_StartServers_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            ClearBlockContents();

            var profileList = new List<ServerProfileSnapshot>();

            foreach (var server in serverList)
            {
                // check if another command is being run against the profile
                if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                {
                    AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ServerName));
                    continue;
                }

                switch (server.Runtime.Status)
                {
                    case ServerStatus.Initializing:
                    case ServerStatus.Stopping:
                    case ServerStatus.Uninstalled:
                    case ServerStatus.Unknown:
                        AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                        continue;

                    case ServerStatus.Running:
                        if (!restart)
                        {
                            AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                            continue;
                        }
                        break;

                    case ServerStatus.Updating:
                        AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
                        continue;
                }

                _currentProfileCommands.Add(server.Profile.ProfileID, restart ? CommandType.Restart : CommandType.Start);
                var profile = ServerProfileSnapshot.Create(server.Profile);
                profileList.Add(profile);
            }

            CancellationTokenSource = new CancellationTokenSource();
            var token = CancellationTokenSource.Token;
            var tasks = new List<Task>();

            foreach (var profile in profileList)
            {
                var app = new ServerApp(true)
                {
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ShutdownReason = shutdownReason,
                    ServerProcess = ServerProcessType.Restart,
                    RestartIfShutdown = true,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.FirstOrDefault(s => string.Equals(profile.ProfileId, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase));
                            if (server != null)
                            {
                                server.Runtime.UpdateServerStatus(serverStatus, serverStatus != ServerStatus.Unknown);
                            }
                        }).Wait(token);
                    }
                };

                var task = new Task(() =>
                {
                    if (restart)
                        AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_RestartRequested"), profile.ServerName));
                    else
                        AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_StartRequested"), profile.ServerName));

                    app.PerformProfileShutdown(profile, true, ServerUpdateType.None, false, false, token);

                    Task.Delay(DELAY_PROCESSCOMPLETE).Wait();

                    AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_CommandComplete"), profile.ServerName));
                    _currentProfileCommands.Remove(profile.ProfileId);
                }, token);

                tasks.Add(task);
            }

            try
            {
                if (processServersSequentially)
                {
                    foreach (var task in tasks)
                    {
                        token.ThrowIfCancellationRequested();

                        task.Start();
                        await task;

                        await Task.Delay(sequentialProcessDelay * 1000, token);
                    }
                }
                else
                {
                    foreach (var task in tasks)
                    {
                        token.ThrowIfCancellationRequested();

                        task.Start();
                    }
                    await Task.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException)
            {
                AddCriticalBlockContent(_globalizer.GetResourceString("ServerMonitor_ProcessServer_CancelledLabel"));
            }
            catch (Exception)
            {
            }
            finally
            {
                _currentProfileCommands.Clear();
                CancellationTokenSource?.Dispose();
                CancellationTokenSource = null;
            }
        }

        private async Task StopSelectedServersAsync(bool shutdown, bool processServersSequentially, int sequentialProcessDelay, string shutdownReason)
        {
            if (CancellationTokenSource != null)
                return;

            var serverList = ServerManager.Servers.Where(s => s.Selected);
            if (serverList.IsEmpty())
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_NoServersSelected_ErrorLabel"), _globalizer.GetResourceString("ServerMonitor_NoServersSelected_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = shutdown
                ? MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_ShutdownServers_ConfirmLabel"), _globalizer.GetResourceString("ServerMonitor_ShutdownServers_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question)
                : MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_StopServers_ConfirmLabel"), _globalizer.GetResourceString("ServerMonitor_StopServers_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            ClearBlockContents();

            var profileList = new List<ServerProfileSnapshot>();

            foreach (var server in serverList)
            {
                // check if another command is being run against the profile
                if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                {
                    AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ServerName));
                    continue;
                }

                switch (server.Runtime.Status)
                {
                    case ServerStatus.Stopping:
                    case ServerStatus.Stopped:
                    case ServerStatus.Uninstalled:
                    case ServerStatus.Unknown:
                        AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                        continue;

                    case ServerStatus.Initializing:
                        if (shutdown)
                        {
                            AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                            continue;
                        }
                        break;

                    case ServerStatus.Updating:
                        AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
                        continue;
                }

                _currentProfileCommands.Add(server.Profile.ProfileID, CommandType.Stop);
                profileList.Add(ServerProfileSnapshot.Create(server.Profile));
            }

            CancellationTokenSource = new CancellationTokenSource();
            var token = CancellationTokenSource.Token;
            var tasks = new List<Task>();

            foreach (var profile in profileList)
            {
                var app = new ServerApp(true)
                {
                    BackupWorldFile = shutdown,
                    OutputLogs = false,
                    PerformWorldSave = shutdown,
                    SendAlerts = true,
                    SendEmails = false,
                    ShutdownReason = shutdownReason,
                    ServerProcess = shutdown ? ServerProcessType.Shutdown : ServerProcessType.Stop,
                    RestartIfShutdown = false,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.FirstOrDefault(s => string.Equals(profile.ProfileId, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase));
                            if (server != null)
                            {
                                server.Runtime.UpdateServerStatus(serverStatus, serverStatus != ServerStatus.Unknown);
                            }
                        }).Wait(token);
                    }
                };

                if (!shutdown)
                    app.ShutdownInterval = 0;

                var task = new Task(() =>
                {
                    if (shutdown)
                        AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ShutdownRequested"), profile.ServerName));
                    else
                        AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_StopRequested"), profile.ServerName));

                    app.PerformProfileShutdown(profile, false, ServerUpdateType.None, false, false, token);

                    Task.Delay(DELAY_PROCESSCOMPLETE).Wait();

                    AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_CommandComplete"), profile.ServerName));
                    _currentProfileCommands.Remove(profile.ProfileId);
                }, token);

                tasks.Add(task);
            }

            try
            {
                if (processServersSequentially)
                {
                    foreach (var task in tasks)
                    {
                        token.ThrowIfCancellationRequested();

                        task.Start();
                        await task;

                        await Task.Delay(sequentialProcessDelay * 1000, token);
                    }
                }
                else
                {
                    foreach (var task in tasks)
                    {
                        token.ThrowIfCancellationRequested();

                        task.Start();
                    }
                    await Task.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException)
            {
                AddCriticalBlockContent(_globalizer.GetResourceString("ServerMonitor_ProcessServer_CancelledLabel"));
            }
            catch (Exception)
            {
            }
            finally
            {
                _currentProfileCommands.Clear();
                CancellationTokenSource?.Dispose();
                CancellationTokenSource = null;
            }
        }

        private async Task UpdateSelectedServersAsync(bool updateModsOnly, bool processServersSequentially, int sequentialProcessDelay, string shutdownReason)
        {
            if (CancellationTokenSource != null)
                return;

            var serverList = ServerManager.Servers.Where(s => s.Selected);
            if (serverList.IsEmpty())
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_NoServersSelected_ErrorLabel"), _globalizer.GetResourceString("ServerMonitor_NoServersSelected_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(_globalizer.GetResourceString("ServerMonitor_UpdateServers_ConfirmLabel"), _globalizer.GetResourceString("ServerMonitor_UpdateServers_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            ClearBlockContents();

            var profileList = new List<ServerProfileSnapshot>();

            foreach (var server in serverList)
            {
                // check if another command is being run against the profile
                if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                {
                    AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ServerName));
                    continue;
                }

                switch (server.Runtime.Status)
                {
                    case ServerStatus.Running:
                        break;

                    case ServerStatus.Initializing:
                    case ServerStatus.Stopping:
                    case ServerStatus.Unknown:
                        AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                        continue;

                    case ServerStatus.Updating:
                        AddErrorBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
                        continue;
                }

                _currentProfileCommands.Add(server.Profile.ProfileID, CommandType.Update);
                var profile = ServerProfileSnapshot.Create(server.Profile);
                profileList.Add(profile);
            }

            CancellationTokenSource = new CancellationTokenSource();
            var token = CancellationTokenSource.Token;
            var tasks = new List<Task>();

            foreach (var profile in profileList)
            {
                var app = new ServerApp(true)
                {
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ShutdownReason = shutdownReason,
                    ServerProcess = ServerProcessType.Update,
                    RestartIfShutdown = false,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.FirstOrDefault(s => string.Equals(profile.ProfileId, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase));
                            if (server != null)
                            {
                                server.Runtime.UpdateServerStatus(serverStatus, serverStatus != ServerStatus.Unknown);
                            }
                        }).Wait(token);
                    }
                };

                var task = new Task(() =>
                {
                    AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_UpdateRequested"), profile.ServerName));

                    app.PerformProfileShutdown(profile, true, updateModsOnly ? ServerUpdateType.Mods : ServerUpdateType.ServerAndMods, false, false, token);

                    Task.Delay(DELAY_PROCESSCOMPLETE).Wait();

                    AddMessageBlockContent(string.Format(_globalizer.GetResourceString("DiscordBot_CommandComplete"), profile.ServerName));
                    _currentProfileCommands.Remove(profile.ProfileId);
                }, token);

                tasks.Add(task);
            }

            try
            {
                if (processServersSequentially)
                {
                    foreach (var task in tasks)
                    {
                        token.ThrowIfCancellationRequested();

                        task.Start();
                        await task;

                        await Task.Delay(sequentialProcessDelay * 1000, token);
                    }
                }
                else
                {
                    foreach (var task in tasks)
                    {
                        token.ThrowIfCancellationRequested();

                        task.Start();
                    }
                    await Task.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException)
            {
                AddCriticalBlockContent(_globalizer.GetResourceString("ServerMonitor_ProcessServer_CancelledLabel"));
            }
            catch (Exception)
            {
            }
            finally
            {
                _currentProfileCommands.Clear();
                CancellationTokenSource?.Dispose();
                CancellationTokenSource = null;
            }
        }
    }
}
