using NLog;
using ServerManagerTool.Common;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib.Model;
using ServerManagerTool.Plugin.Common;
using ServerManagerTool.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib
{
    public class ServerRuntime : DependencyObject, IDisposable
    {
        private const int DIRECTORIES_PER_LINE = 200;
        private const int MOD_STATUS_QUERY_DELAY = 900000; // milliseconds
        private const int RCON_MAXRETRIES = 3;

        public event EventHandler StatusUpdate;

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private readonly List<PropertyChangeNotifier> profileNotifiers = new List<PropertyChangeNotifier>();
        private Process serverProcess;
        private IAsyncDisposable updateRegistration;
        private DateTime lastModStatusQuery = DateTime.MinValue;
        private System.Timers.Timer motdIntervalTimer = new System.Timers.Timer(3600000);
        private QueryMaster.Rcon _rconConsole = null;

        #region Properties

        public static readonly DependencyProperty AvailabilityProperty = DependencyProperty.Register(nameof(Availability), typeof(AvailabilityStatus), typeof(ServerRuntime), new PropertyMetadata(AvailabilityStatus.Unknown));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(ServerStatus), typeof(ServerRuntime), new PropertyMetadata(ServerStatus.Unknown));
        public static readonly DependencyProperty StatusStringProperty = DependencyProperty.Register(nameof(StatusString), typeof(string), typeof(ServerRuntime), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty MaxPlayersProperty = DependencyProperty.Register(nameof(MaxPlayers), typeof(int), typeof(ServerRuntime), new PropertyMetadata(0));
        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register(nameof(Players), typeof(int), typeof(ServerRuntime), new PropertyMetadata(0));
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(nameof(Version), typeof(Version), typeof(ServerRuntime), new PropertyMetadata(new Version()));
        public static readonly DependencyProperty ProfileSnapshotProperty = DependencyProperty.Register(nameof(ProfileSnapshot), typeof(ServerProfileSnapshot), typeof(ServerRuntime), new PropertyMetadata(null));
        public static readonly DependencyProperty TotalModCountProperty = DependencyProperty.Register(nameof(TotalModCount), typeof(int), typeof(ServerRuntime), new PropertyMetadata(0));
        public static readonly DependencyProperty OutOfDateModCountProperty = DependencyProperty.Register(nameof(OutOfDateModCount), typeof(int), typeof(ServerRuntime), new PropertyMetadata(0));

        public AvailabilityStatus Availability
        {
            get { return (AvailabilityStatus)GetValue(AvailabilityProperty); }
            protected set { SetValue(AvailabilityProperty, value); }
        }

        public ServerStatus Status
        {
            get { return (ServerStatus)GetValue(StatusProperty); }
            protected set { SetValue(StatusProperty, value); }
        }

        public string StatusString
        {
            get { return (string)GetValue(StatusStringProperty); }
            protected set { SetValue(StatusStringProperty, value); }
        }

        public int MaxPlayers
        {
            get { return (int)GetValue(MaxPlayersProperty); }
            protected set { SetValue(MaxPlayersProperty, value); }
        }

        public int Players
        {
            get { return (int)GetValue(PlayersProperty); }
            protected set { SetValue(PlayersProperty, value); }
        }

        public Version Version
        {
            get { return (Version)GetValue(VersionProperty); }
            protected set { SetValue(VersionProperty, value); }
        }

        public ServerProfileSnapshot ProfileSnapshot
        {
            get { return (ServerProfileSnapshot)GetValue(ProfileSnapshotProperty); }
            set { SetValue(ProfileSnapshotProperty, value); }
        }

        public int TotalModCount
        {
            get { return (int)GetValue(TotalModCountProperty); }
            protected set { SetValue(TotalModCountProperty, value); }
        }

        public int OutOfDateModCount
        {
            get { return (int)GetValue(OutOfDateModCountProperty); }
            protected set { SetValue(OutOfDateModCountProperty, value); }
        }

        public static bool EnableUpdateModStatus { get; set; } = true;

        #endregion

        public void Dispose()
        {
            if (this.motdIntervalTimer != null)
            {
                this.motdIntervalTimer.Stop();
                this.motdIntervalTimer.Elapsed -= async (sender, e) => await HandleMotDIntervalTimer();
                this.motdIntervalTimer.Dispose();
                this.motdIntervalTimer = null;
            }

            this.updateRegistration?.DisposeAsync().DoNotWait();
        }

        public Task AttachToProfile(ServerProfile profile)
        {
            AttachToProfileCore(profile);
            GetProfilePropertyChanges(profile);
            return TaskUtils.FinishedTask;
        }

        private void AttachToProfileCore(ServerProfile profile)
        {
            UnregisterForUpdates();

            if (this.ProfileSnapshot == null)
                this.lastModStatusQuery = DateTime.MinValue;
            this.ProfileSnapshot = ServerProfileSnapshot.Create(profile);

            // setup the MotD timer
            this.motdIntervalTimer = new System.Timers.Timer(this.ProfileSnapshot.MOTDInterval * 60 * 1000);
            this.motdIntervalTimer.Elapsed += async (sender, e) => await HandleMotDIntervalTimer();

            if (Version.TryParse(profile.LastInstalledVersion, out Version lastInstalled))
            {
                this.Version = lastInstalled;
            }

            RegisterForUpdates();
        }

        private void GetProfilePropertyChanges(ServerProfile profile)
        {
            foreach(var notifier in profileNotifiers)
            {
                notifier.Dispose();
            }

            profileNotifiers.Clear();
            profileNotifiers.AddRange(PropertyChangeNotifier.GetNotifiers(
                profile,
                new[] {
                    ServerProfile.ProfileNameProperty,
                    ServerProfile.InstallDirectoryProperty,
                    ServerProfile.ServerPortProperty,
                    ServerProfile.QueryPortProperty,
                    ServerProfile.ServerIPProperty,
                    ServerProfile.MaxPlayersProperty,

                    ServerProfile.ServerPasswordProperty,
                    ServerProfile.AdminPasswordProperty,

                    ServerProfile.ServerMapProperty,
                    ServerProfile.ServerModIdsProperty,
                    ServerProfile.TotalConversionModIdProperty,

                    ServerProfile.MOTDIntervalProperty,
                },
                (s, p) =>
                {
                    if (Status == ServerStatus.Stopped || Status == ServerStatus.Uninstalled || Status == ServerStatus.Unknown || Status == ServerStatus.Updating)
                    {
                        AttachToProfileCore(profile);
                    }
                }));
        }

        private void GetServerEndpoints(out IPEndPoint localServerQueryEndPoint, out IPEndPoint steamServerQueryEndPoint)
        {
            localServerQueryEndPoint = null;
            steamServerQueryEndPoint = null;

            //
            // Get the local endpoint for querying the local network
            //
            if (!ushort.TryParse(this.ProfileSnapshot.QueryPort.ToString(), out _))
            {
                _logger.Error($"Port is out of range ({this.ProfileSnapshot.QueryPort})");
                return;
            }

            localServerQueryEndPoint = new IPEndPoint(this.ProfileSnapshot.ServerIPAddress, Convert.ToUInt16(this.ProfileSnapshot.QueryPort));

            //
            // Get the public endpoint for querying Steam
            //
            steamServerQueryEndPoint = null;
            if (!string.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
            {
                if (IPAddress.TryParse(Config.Default.MachinePublicIP, out IPAddress steamServerIpAddress))
                {
                    // Use the Public IP explicitly specified
                    steamServerQueryEndPoint = new IPEndPoint(steamServerIpAddress, Convert.ToUInt16(this.ProfileSnapshot.QueryPort));
                }
                else
                {
                    // Resolve the IP from the DNS name provided
                    try
                    {
                        var addresses = Dns.GetHostAddresses(Config.Default.MachinePublicIP);
                        if (addresses.Length > 0)
                        {
                            steamServerQueryEndPoint = new IPEndPoint(addresses[0], Convert.ToUInt16(this.ProfileSnapshot.QueryPort));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"{nameof(GetServerEndpoints)} - Failed to resolve DNS address {Config.Default.MachinePublicIP}. {ex.Message}\r\n{ex.StackTrace}");
                    }
                }
            }
        }

        public string GetServerExe() => Path.Combine(this.ProfileSnapshot.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe);

        public string GetServerLauncherFile() => Path.Combine(this.ProfileSnapshot.InstallDirectory, Config.Default.ServerConfigRelativePath, Config.Default.LauncherFile);

        private void ProcessStatusUpdate(IAsyncDisposable registration, ServerStatusUpdate update)
        {
            if (!ReferenceEquals(registration, this.updateRegistration))
            {
                return;
            }

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var oldStatus = this.Status;
                var oldAvailability = this.Availability;

                switch (update.Status)
                {
                    case WatcherServerStatus.Unknown:
                        if (oldStatus != ServerStatus.Updating)
                        {
                            UpdateServerStatus(
                                ServerStatus.Unknown, 
                                AvailabilityStatus.Unknown, 
                                false);
                        }

                        if (this.motdIntervalTimer != null && this.motdIntervalTimer.Enabled)
                        {
                            this.motdIntervalTimer.Stop();
                        }

                        break;

                    case WatcherServerStatus.NotInstalled:
                        if (oldStatus != ServerStatus.Updating)
                        {
                            UpdateServerStatus(
                                ServerStatus.Uninstalled, 
                                AvailabilityStatus.Unavailable, 
                                false);
                        }

                        if (this.motdIntervalTimer != null && this.motdIntervalTimer.Enabled)
                        {
                            this.motdIntervalTimer.Stop();
                        }

                        break;

                    case WatcherServerStatus.Stopped:
                        if (oldStatus != ServerStatus.Updating)
                        {
                            UpdateServerStatus(
                                ServerStatus.Stopped, 
                                AvailabilityStatus.Unavailable, 
                                oldStatus == ServerStatus.Initializing || oldStatus == ServerStatus.Running || oldStatus == ServerStatus.Stopping);
                        }

                        if (this.motdIntervalTimer != null && this.motdIntervalTimer.Enabled)
                        {
                            this.motdIntervalTimer.Stop();
                        }

                        break;

                    case WatcherServerStatus.Initializing:
                        if (oldStatus != ServerStatus.Stopping)
                        {
                            UpdateServerStatus(
                                ServerStatus.Initializing, 
                                AvailabilityStatus.Unavailable, 
                                oldStatus != ServerStatus.Initializing && oldStatus != ServerStatus.Unknown);
                        }

                        if (this.motdIntervalTimer != null && this.motdIntervalTimer.Enabled)
                        {
                            this.motdIntervalTimer.Stop();
                        }

                        break;

                    case WatcherServerStatus.LocalSuccess:
                        if (oldStatus != ServerStatus.Stopping)
                        {
                            UpdateServerStatus(
                                ServerStatus.Running,
                                AvailabilityStatus.LocalOnly,
                                oldStatus != ServerStatus.Running && oldStatus != ServerStatus.Unknown);

                            if (this.ProfileSnapshot.MOTDIntervalEnabled && this.motdIntervalTimer != null && !this.motdIntervalTimer.Enabled)
                            {
                                this.motdIntervalTimer.Start();
                            }
                        }
                        else
                        {
                            if (this.motdIntervalTimer != null && this.motdIntervalTimer.Enabled)
                            {
                                this.motdIntervalTimer.Stop();
                            }
                        }
                        break;

                    case WatcherServerStatus.ExternalSkipped:
                        if (oldStatus != ServerStatus.Stopping)
                        {
                            UpdateServerStatus(
                                ServerStatus.Running,
                                oldAvailability >= AvailabilityStatus.PublicOnly ? AvailabilityStatus.PublicOnly : AvailabilityStatus.LocalOnly,
                                oldStatus != ServerStatus.Running && oldStatus != ServerStatus.Unknown);

                            if (this.ProfileSnapshot.MOTDIntervalEnabled && this.motdIntervalTimer != null && !this.motdIntervalTimer.Enabled)
                            {
                                this.motdIntervalTimer.Start();
                            }
                        }
                        else
                        {
                            if (this.motdIntervalTimer != null && this.motdIntervalTimer.Enabled)
                            {
                                this.motdIntervalTimer.Stop();
                            }
                        }
                        break;

                    case WatcherServerStatus.ExternalSuccess:
                        if (oldStatus != ServerStatus.Stopping)
                        {
                            UpdateServerStatus(
                                ServerStatus.Running,
                                AvailabilityStatus.PublicOnly,
                                oldStatus != ServerStatus.Running && oldStatus != ServerStatus.Unknown);

                            if (this.ProfileSnapshot.MOTDIntervalEnabled && this.motdIntervalTimer != null && !this.motdIntervalTimer.Enabled)
                            {
                                this.motdIntervalTimer.Start();
                            }
                        }
                        else
                        {
                            if (this.motdIntervalTimer != null && this.motdIntervalTimer.Enabled)
                            {
                                this.motdIntervalTimer.Stop();
                            }
                        }
                        break;

                    case WatcherServerStatus.Published:
                        if (oldStatus != ServerStatus.Stopping)
                        {
                            UpdateServerStatus(
                                ServerStatus.Running, 
                                AvailabilityStatus.Available, 
                                oldStatus != ServerStatus.Running && oldStatus != ServerStatus.Unknown);

                            if (this.ProfileSnapshot.MOTDIntervalEnabled && this.motdIntervalTimer != null && !this.motdIntervalTimer.Enabled)
                            {
                                this.motdIntervalTimer.Start();
                            }
                        }
                        else
                        {
                            if (this.motdIntervalTimer != null && this.motdIntervalTimer.Enabled)
                            {
                                this.motdIntervalTimer.Stop();
                            }
                        }
                        break;

                    default:
                        if (this.motdIntervalTimer != null && this.motdIntervalTimer.Enabled)
                        {
                            this.motdIntervalTimer.Stop();
                        }

                        break;
                }

                var previousOnlinePlayerCount = this.Players;
                this.Players = update.OnlinePlayerCount;
                this.MaxPlayers = update.ServerInfo?.MaxPlayers ?? this.ProfileSnapshot.MaxPlayerCount;

                if (previousOnlinePlayerCount != this.Players)
                {
                    PluginHelper.Instance.ProcessAlert(AlertType.OnlinePlayerCountChanged, this.ProfileSnapshot.ProfileName, $"{Config.Default.Alert_OnlinePlayerCountChange} {this.Players} / {this.MaxPlayers}");
                }

                if (update.ServerInfo != null)
                {
                    var match = Regex.Match(update.ServerInfo.Name, @"\(v([0-9]+\.[0-9]*)\)");
                    if (match.Success && match.Groups.Count >= 2)
                    {
                        var serverVersion = match.Groups[1].Value;
                        if (!string.IsNullOrWhiteSpace(serverVersion) && Version.TryParse(serverVersion, out Version temp))
                        {
                            this.Version = temp;
                        }
                    }
                }

                UpdateModStatus();

                this.serverProcess = update.Process;

                StatusUpdate?.Invoke(this, EventArgs.Empty);
            }).DoNotWait();
        }

        private async void UpdateModStatus()
        {
            if (!EnableUpdateModStatus || DateTime.Now < this.lastModStatusQuery.AddMilliseconds(MOD_STATUS_QUERY_DELAY))
                return;

            var totalModCount = 0;
            var outOfdateModCount = 0;

            var modIdList = new List<string>();
            if (!string.IsNullOrWhiteSpace(this.ProfileSnapshot.ServerMapModId))
                modIdList.Add(this.ProfileSnapshot.ServerMapModId);
            if (!string.IsNullOrWhiteSpace(this.ProfileSnapshot.TotalConversionModId))
                modIdList.Add(this.ProfileSnapshot.TotalConversionModId);
            modIdList.AddRange(this.ProfileSnapshot.ServerModIds);

            modIdList = ModUtils.ValidateModList(modIdList);
            totalModCount = modIdList.Count;

            if (totalModCount > 0)
            {
                var response = await Task.Run(() => SteamUtils.GetSteamModDetails(modIdList));
                var modsRootFolder = Path.Combine(this.ProfileSnapshot.InstallDirectory, Config.Default.ServerModsRelativePath);
                var modDetails = ModDetailList.GetModDetails(modIdList, modsRootFolder, null, response);

                outOfdateModCount = modDetails.Count(m => m.IsValid && !m.UpToDate);
            }

            if (outOfdateModCount > 0 && this.OutOfDateModCount != outOfdateModCount && !string.IsNullOrWhiteSpace(Config.Default.Alert_ModUpdateDetected))
            {
                PluginHelper.Instance.ProcessAlert(AlertType.ModUpdateDetected, this.ProfileSnapshot.ProfileName, $"{Config.Default.Alert_ModUpdateDetected} {outOfdateModCount}");
            }

            this.TotalModCount = totalModCount;
            this.OutOfDateModCount = outOfdateModCount;
            this.lastModStatusQuery = DateTime.Now;

            _logger.Debug($"{nameof(UpdateModStatus)} performed - {this.ProfileSnapshot.ProfileName} - {outOfdateModCount} / {totalModCount}");
        }

        private void RegisterForUpdates()
        {
            if (this.updateRegistration == null)
            {
                GetServerEndpoints(out IPEndPoint localServerQueryEndPoint, out IPEndPoint steamServerQueryEndPoint);
                if (localServerQueryEndPoint == null || steamServerQueryEndPoint == null)
                    return;

                this.updateRegistration = ServerStatusWatcher.Instance.RegisterForUpdates(this.ProfileSnapshot.InstallDirectory, this.ProfileSnapshot.ProfileId, localServerQueryEndPoint, steamServerQueryEndPoint, ProcessStatusUpdate);
            }
        }

        private void UnregisterForUpdates()
        {
            this.motdIntervalTimer.Stop();
            this.motdIntervalTimer.Elapsed -= async (sender, e) => await HandleMotDIntervalTimer();

            this.updateRegistration?.DisposeAsync().DoNotWait();
            this.updateRegistration = null;
        }


        public Task StartAsync()
        {
            if(!Environment.Is64BitOperatingSystem)
            {
                MessageBox.Show("The server requires a 64-bit operating system to run. Your operating system is 32-bit and therefore the Ark Server Manager cannot start the server. You may still load and save profiles and settings files for use on other machines.", "64-bit OS Required", MessageBoxButton.OK, MessageBoxImage.Error);
                return TaskUtils.FinishedTask;
            }

            switch(this.Status)
            {
                case ServerStatus.Running:
                case ServerStatus.Initializing:
                case ServerStatus.Stopping:
                    _logger.Debug($"{nameof(StartAsync)} - Server {this.ProfileSnapshot.ProfileName} already running.");
                    return TaskUtils.FinishedTask;
            }

            UnregisterForUpdates();

            var serverExe = GetServerExe();
            var launcherExe = GetServerLauncherFile();

            if (Config.Default.ManageFirewallAutomatically)
            {
                if (SecurityUtils.IsAdministrator())
                {
                    var ports = new List<int>() { this.ProfileSnapshot.ServerPort, this.ProfileSnapshot.ServerPeerPort, this.ProfileSnapshot.QueryPort };
                    if (this.ProfileSnapshot.RCONEnabled)
                    {
                        ports.Add(this.ProfileSnapshot.RCONPort);
                    }

                    if (!FirewallUtils.CreateFirewallRules(serverExe, ports, $"{Config.Default.FirewallRulePrefix} {this.ProfileSnapshot.ServerName}", description: "", group: Config.Default.FirewallRulePrefix.Replace(":", "")))
                    {
                        var result = MessageBox.Show("Failed to automatically set firewall rules. If you are running custom firewall software, you may need to set your firewall rules manually. You may turn off automatic firewall management in Settings.\r\n\r\nWould you like to continue running the server anyway?", "Automatic Firewall Management Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                        {
                            return TaskUtils.FinishedTask;
                        }
                    }
                }
                else
                {
                    var result = MessageBox.Show("Failed to automatically set firewall rules. You must be running the server manager as an administrator to manage the firewall rules.\r\n\r\nWould you like to continue running the server anyway?", "Automatic Firewall Management Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                    {
                        return TaskUtils.FinishedTask;
                    }
                }
            }

            CheckServerWorldFileExists();
            UpdateServerStatus(ServerStatus.Initializing, false);

            try
            {
                var startInfo = new ProcessStartInfo()
                {
                    FileName = launcherExe
                };

                var process = Process.Start(startInfo);
                process.EnableRaisingEvents = true;
            }
            catch (Win32Exception ex)
            {
                UpdateServerStatus(ServerStatus.Unknown, AvailabilityStatus.Unavailable, false);
                throw new FileNotFoundException($"Unable to find {Config.Default.LauncherFile} at {launcherExe}. Server Install Directory: {this.ProfileSnapshot.InstallDirectory}", launcherExe, ex);
            }
            finally
            {
                RegisterForUpdates();
            }
            
            return TaskUtils.FinishedTask;            
        }

        public async Task StopAsync()
        {
            switch(this.Status)
            {
                case ServerStatus.Running:
                case ServerStatus.Initializing:
                    try
                    {
                        if (this.serverProcess != null)
                        {
                            UpdateServerStatus(ServerStatus.Stopping, AvailabilityStatus.Unavailable, true);

                            await ProcessUtils.SendStopAsync(this.serverProcess)
                                .ContinueWith(async t1 =>
                                {
                                    if (this.serverProcess.HasExited)
                                    {
                                        await TaskUtils.RunOnUIThreadAsync(() => CheckServerWorldFileExists());
                                    }
                                })
                                .ContinueWith(async t2 =>
                                {
                                    await TaskUtils.RunOnUIThreadAsync(() => UpdateServerStatus(ServerStatus.Stopped, AvailabilityStatus.Unavailable, false));
                                });
                        }
                    }
                    catch(InvalidOperationException)
                    {
                        UpdateServerStatus(ServerStatus.Unknown, AvailabilityStatus.Unavailable, false);
                    }
                    break;
            }            
        }

        public async Task<bool> UpgradeAsync(CancellationToken cancellationToken, bool updateServer, BranchSnapshot branch, bool validate, bool updateMods, ProgressDelegate progressCallback)
        {
            return await UpgradeAsync(cancellationToken, updateServer, branch, validate, updateMods, null, progressCallback);
        }

        public async Task<bool> UpgradeAsync(CancellationToken cancellationToken, bool updateServer, BranchSnapshot branch, bool validate, bool updateMods, string[] updateModIds, ProgressDelegate progressCallback)
        {
            if (updateServer && !Environment.Is64BitOperatingSystem)
            {
                var result = MessageBox.Show("The server requires a 64-bit operating system to run. Your operating system is 32-bit and therefore the Ark Server Manager will be unable to start the server, but you may still install it or load and save profiles and settings files for use on other machines.\r\n\r\nDo you wish to continue?", "64-bit OS Required", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }

            try
            {
                await StopAsync();

                bool isNewInstallation = this.Status == ServerStatus.Uninstalled;

                UpdateServerStatus(ServerStatus.Updating, false);

                // Run the SteamCMD to install the server
                var steamCmdFile = SteamCmdUpdater.GetSteamCmdFile(Config.Default.DataDir);
                if (string.IsNullOrWhiteSpace(steamCmdFile) || !File.Exists(steamCmdFile))
                {
                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***********************************");
                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: SteamCMD could not be found. Expected location is {steamCmdFile}");
                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***********************************");
                    return false;
                }

                // record the start time of the process, this is used to determine if any files changed in the download process.
                var startTime = DateTime.Now;

                var gotNewVersion = false;
                var downloadSuccessful = false;
                var success = false;

                if (updateServer)
                {
                    // *********************
                    // Server Update Section
                    // *********************

                    var branchInfo = ServerApp.GetBranchInfo(branch?.AppIdServer, branch?.BranchName);

                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Starting server update.");
                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Server branch: {branchInfo}.");
                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Profile name: {this.ProfileSnapshot.ProfileName}.");

                    // create the branch arguments
                    var steamCmdInstallServerBetaArgs = new StringBuilder();
                    if (!string.IsNullOrWhiteSpace(branch?.BranchName))
                    {
                        steamCmdInstallServerBetaArgs.AppendFormat(Config.Default.SteamCmdInstallServerBetaNameArgsFormat, branch.BranchName);
                        if (!string.IsNullOrWhiteSpace(branch?.BranchPassword))
                        {
                            steamCmdInstallServerBetaArgs.Append(" ");
                            steamCmdInstallServerBetaArgs.AppendFormat(Config.Default.SteamCmdInstallServerBetaPasswordArgsFormat, branch?.BranchPassword);
                        }
                    }

                    // Check if this is a new server installation.
                    if (isNewInstallation && Config.Default.AutoUpdate_EnableUpdate && !string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_CacheDir))
                    {
                        var cacheFolder = ServerApp.GetServerCacheFolder(branch?.AppIdServer, branch?.BranchName);

                        // check if the auto-update facility is enabled and the cache folder defined.
                        if (!string.IsNullOrWhiteSpace(cacheFolder) && Directory.Exists(cacheFolder))
                        {
                            // Auto-Update enabled and cache foldler exists.
                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Installing server from local cache...may take a while to copy all the files.");

                            // Install the server files from the cache.
                            var installationFolder = this.ProfileSnapshot.InstallDirectory;
                            int count = 0;
                            await Task.Run(() =>
                                ServerApp.DirectoryCopy(cacheFolder, installationFolder, true, Config.Default.AutoUpdate_UseSmartCopy, (p, m, n) =>
                                {
                                    count++;
                                    progressCallback?.Invoke(0, ".", count % DIRECTORIES_PER_LINE == 0);
                                }), cancellationToken);
                        }
                    }

                    progressCallback?.Invoke(0, "", true);
                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Updating server from steam.");

                    downloadSuccessful = !Config.Default.SteamCmdRedirectOutput;
                    DataReceivedEventHandler serverOutputHandler = (s, e) =>
                    {
                        var dataValue = e.Data ?? string.Empty;
                        progressCallback?.Invoke(0, dataValue);
                        if (!gotNewVersion && dataValue.Contains("downloading,"))
                        {
                            gotNewVersion = true;
                        }
                        if (dataValue.StartsWith("Success!"))
                        {
                            downloadSuccessful = true;
                        }
                    };

                    var steamCmdRemoveQuit = CommonConfig.Default.SteamCmdRemoveQuit && !Config.Default.SteamCmdRedirectOutput;
                    var steamCmdArgs = SteamUtils.BuildSteamCmdArguments(steamCmdRemoveQuit, Config.Default.SteamCmdInstallServerArgsFormat, Config.Default.SteamCmd_AnonymousUsername, this.ProfileSnapshot.InstallDirectory, this.ProfileSnapshot.AppIdServer, steamCmdInstallServerBetaArgs.ToString(), validate ? "validate" : string.Empty);
                    var workingDirectory = Config.Default.DataDir;

                    var SteamCmdIgnoreExitStatusCodes = SteamUtils.GetExitStatusList(Config.Default.SteamCmdIgnoreExitStatusCodes);

                    success = await ServerUpdater.UpgradeServerAsync(steamCmdFile, steamCmdArgs, workingDirectory, null, null, this.ProfileSnapshot.InstallDirectory, SteamCmdIgnoreExitStatusCodes, Config.Default.SteamCmdRedirectOutput ? serverOutputHandler : null, cancellationToken, steamCmdRemoveQuit ? ProcessWindowStyle.Normal : ProcessWindowStyle.Minimized);
                    if (success && downloadSuccessful)
                    {
                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Finished server update.");

                        if (Directory.Exists(this.ProfileSnapshot.InstallDirectory))
                        {
                            if (!Config.Default.SteamCmdRedirectOutput)
                                // check if any of the server files have changed.
                                gotNewVersion = ServerApp.HasNewServerVersion(this.ProfileSnapshot.InstallDirectory, startTime);

                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} New server version - {gotNewVersion.ToString().ToUpperInvariant()}.");

                            // update the version number of the server.
                            var versionFile = Path.Combine(this.ProfileSnapshot.InstallDirectory, Config.Default.VersionFile);
                            this.Version = ServerApp.GetServerVersion(versionFile);

                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Server version: {this.Version}\r\n");
                        }
                    }
                    else
                    {
                        success = false;
                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ****************************");
                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: Failed server update.");
                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ****************************");
                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Check steamcmd logs for more information why the server update failed.\r\n");

                        if (Config.Default.SteamCmdRedirectOutput)
                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} If the server update keeps failing try disabling the '{_globalizer.GetResourceString("GlobalSettings_SteamCmdRedirectOutputLabel")}' option in the settings window.\r\n");
                    }
                }
                else
                    success = true;

                if (success)
                {
                    if (updateMods)
                    {
                        // ******************
                        // Mod Update Section
                        // ******************

                        // build a list of mods to be processed
                        var modIdList = new List<string>();
                        if (updateModIds == null || updateModIds.Length == 0)
                        {
                            if (!string.IsNullOrWhiteSpace(this.ProfileSnapshot.ServerMapModId))
                                modIdList.Add(this.ProfileSnapshot.ServerMapModId);
                            if (!string.IsNullOrWhiteSpace(this.ProfileSnapshot.TotalConversionModId))
                                modIdList.Add(this.ProfileSnapshot.TotalConversionModId);
                            modIdList.AddRange(this.ProfileSnapshot.ServerModIds);
                        }
                        else
                        {
                            modIdList.AddRange(updateModIds);
                        }

                        modIdList = ModUtils.ValidateModList(modIdList);

                        // get the details of the mods to be processed.
                        var modDetails = SteamUtils.GetSteamModDetails(modIdList);
                        var forceUpdateMods = Config.Default.ServerUpdate_ForceUpdateModsIfNoSteamInfo || string.IsNullOrWhiteSpace(SteamUtils.SteamWebApiKey);

                        // check if the mod details were retrieved
                        if (modDetails == null && forceUpdateMods)
                        {
                            modDetails = new PublishedFileDetailsResponse();
                        }

                        if (modDetails != null)
                        {
                            // create a new list for any failed mod updates
                            var failedMods = new List<string>(modIdList.Count);

                            for (var index = 0; index < modIdList.Count; index++)
                            {
                                var modId = modIdList[index];
                                var modTitle = modId;
                                var modSuccess = false;
                                gotNewVersion = false;
                                downloadSuccessful = false;

                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Started processing mod {index + 1} of {modIdList.Count}.");

                                // check if the steam information was downloaded
                                var modDetail = modDetails.publishedfiledetails?.FirstOrDefault(m => m.publishedfileid.Equals(modId, StringComparison.OrdinalIgnoreCase));
                                modTitle = $"{modId} - {modDetail?.title ?? "<unknown>"}";

                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Mod {modTitle}.");

                                var modCacheLastUpdated = 0;
                                var downloadMod = true;
                                var copyMod = true;
                                var updateError = false;

                                if (modDetail?.creator_app_id != null && !modDetail.creator_app_id.Equals(this.ProfileSnapshot.AppId, StringComparison.OrdinalIgnoreCase))
                                {
                                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***************************************************************************");
                                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: Mod cannot be updated, this mod does not belong to this application.");
                                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***************************************************************************");

                                    downloadMod = false;
                                    copyMod = false;
                                    updateError = true;
                                }

                                var modCachePath = ModUtils.GetModCachePath(modId, this.ProfileSnapshot.AppId);
                                var cacheTimeFile = ModUtils.GetLatestModCacheTimeFile(modId, this.ProfileSnapshot.AppId);
                                var modPath = ModUtils.GetModPath(this.ProfileSnapshot.InstallDirectory, modId);
                                var modTimeFile = ModUtils.GetLatestModTimeFile(this.ProfileSnapshot.InstallDirectory, modId);

                                if (downloadMod)
                                {
                                    // check if the mod needs to be downloaded, or force the download.
                                    if (Config.Default.ServerUpdate_ForceUpdateMods)
                                    {
                                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Forcing mod download - ASM setting is TRUE.");
                                    }
                                    else if (modDetail == null)
                                    {
                                        if (forceUpdateMods)
                                        {
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Forcing mod download - Mod details not available and ASM setting is TRUE.");
                                        }
                                        else
                                        {
                                            // no steam information downloaded, display an error, mod might no longer be available
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} *******************************************************************");
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: Mod cannot be updated, unable to download steam information.");
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} *******************************************************************");

                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} If the mod update keeps failing try enabling the '{_globalizer.GetResourceString("GlobalSettings_ForceUpdateModsIfNoSteamInfoLabel")}' option in the settings window.\r\n");

                                            downloadMod = false;
                                            copyMod = false;
                                            updateError = true;
                                        }
                                    }
                                    else
                                    {
                                        // check if the mod detail record is valid (private mod).
                                        if (modDetail.time_updated <= 0)
                                        {
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Forcing mod download - mod is private.");
                                        }
                                        else
                                        {
                                            modCacheLastUpdated = ModUtils.GetModLatestTime(cacheTimeFile);
                                            if (modCacheLastUpdated <= 0)
                                            {
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Forcing mod download - mod cache is not versioned.");
                                            }
                                            else
                                            {
                                                var steamLastUpdated = modDetail.time_updated;
                                                if (steamLastUpdated <= modCacheLastUpdated)
                                                {
                                                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Skipping mod download - mod cache has the latest version.");
                                                    downloadMod = false;
                                                }
                                            }
                                        }
                                    }

                                    if (downloadMod)
                                    {
                                        // mod will be downloaded
                                        downloadSuccessful = !Config.Default.SteamCmdRedirectOutput;
                                        DataReceivedEventHandler modOutputHandler = (s, e) =>
                                        {
                                            var dataValue = e.Data ?? string.Empty;
                                            progressCallback?.Invoke(0, dataValue);
                                            if (dataValue.StartsWith("Success."))
                                            {
                                                downloadSuccessful = true;
                                            }
                                        };

                                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Starting mod download.");

                                        var steamCmdArgs = string.Empty;
                                        var steamCmdRemoveQuit = CommonConfig.Default.SteamCmdRemoveQuit && !Config.Default.SteamCmdRedirectOutput;
                                        if (Config.Default.SteamCmd_UseAnonymousCredentials)
                                            steamCmdArgs = SteamUtils.BuildSteamCmdArguments(steamCmdRemoveQuit, Config.Default.SteamCmdInstallModArgsFormat, Config.Default.SteamCmd_AnonymousUsername, this.ProfileSnapshot.AppId, modId);
                                        else
                                            steamCmdArgs = SteamUtils.BuildSteamCmdArguments(steamCmdRemoveQuit, Config.Default.SteamCmdInstallModArgsFormat, Config.Default.SteamCmd_Username, this.ProfileSnapshot.AppId, modId);
                                        var workingDirectory = Config.Default.DataDir;

                                        var SteamCmdIgnoreExitStatusCodes = SteamUtils.GetExitStatusList(Config.Default.SteamCmdIgnoreExitStatusCodes);

                                        modSuccess = await ServerUpdater.UpgradeModsAsync(steamCmdFile, steamCmdArgs, workingDirectory, null, null, SteamCmdIgnoreExitStatusCodes, Config.Default.SteamCmdRedirectOutput ? modOutputHandler : null, cancellationToken, steamCmdRemoveQuit ? ProcessWindowStyle.Normal : ProcessWindowStyle.Minimized);
                                        if (modSuccess && downloadSuccessful)
                                        {
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Finished mod download.");
                                            copyMod = true;

                                            if (Directory.Exists(modCachePath))
                                            {
                                                // check if any of the mod files have changed.
                                                gotNewVersion = new DirectoryInfo(modCachePath).GetFiles("*.*", SearchOption.AllDirectories).Any(file => file.LastWriteTime >= startTime);

                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} New mod version - {gotNewVersion.ToString().ToUpperInvariant()}.");

                                                var steamLastUpdated = modDetail?.time_updated.ToString() ?? string.Empty;
                                                if (modDetail == null || modDetail.time_updated <= 0)
                                                {
                                                    // get the version number from the steamcmd workshop file.
                                                    steamLastUpdated = ModUtils.GetSteamWorkshopLatestTime(ModUtils.GetSteamWorkshopFile(this.ProfileSnapshot.AppId), modId).ToString();
                                                }

                                                // update the last updated file with the steam updated time.
                                                File.WriteAllText(cacheTimeFile, steamLastUpdated);

                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Mod Cache version: {steamLastUpdated}");
                                            }
                                        }
                                        else
                                        {
                                            modSuccess = false;
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***************************");
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: Mod download failed.");
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***************************");
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Check steamcmd logs for more information why the mod update failed.\r\n");

                                            if (Config.Default.SteamCmdRedirectOutput)
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} If the mod update keeps failing try disabling the '{_globalizer.GetResourceString("GlobalSettings_SteamCmdRedirectOutputLabel")}' option in the settings window.\r\n");
                                            copyMod = false;
                                        }
                                    }
                                    else
                                        modSuccess = !updateError;
                                }
                                else
                                    modSuccess = !updateError;

                                if (copyMod)
                                {
                                    // check if the mod needs to be copied, or force the copy.
                                    if (Config.Default.ServerUpdate_ForceCopyMods)
                                    {
                                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Forcing mod copy - Server Manager setting is TRUE.");
                                    }
                                    else
                                    {
                                        // check the mod version against the cache version.
                                        var modLastUpdated = ModUtils.GetModLatestTime(modTimeFile);
                                        if (modLastUpdated <= 0)
                                        {
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Forcing mod copy - mod is not versioned.");
                                        }
                                        else
                                        {
                                            modCacheLastUpdated = ModUtils.GetModLatestTime(cacheTimeFile);
                                            if (modCacheLastUpdated <= modLastUpdated)
                                            {
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Skipping mod copy - mod has the latest version.");
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Mod version: {modLastUpdated}");
                                                copyMod = false;
                                            }
                                        }
                                    }

                                    if (copyMod)
                                    {
                                        try
                                        {
                                            if (Directory.Exists(modCachePath))
                                            {
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Started mod copy.");
                                                int count = 0;
                                                await Task.Run(() => ModUtils.CopyMod(modCachePath, modPath, modId, (p, m, n) =>
                                                                                                                    {
                                                                                                                        count++;
                                                                                                                        progressCallback?.Invoke(0, ".", count % DIRECTORIES_PER_LINE == 0);
                                                                                                                    }), cancellationToken);
                                                progressCallback?.Invoke(0, "", true);
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Finished mod copy.");

                                                var modLastUpdated = ModUtils.GetModLatestTime(modTimeFile);
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Mod version: {modLastUpdated}");
                                            }
                                            else
                                            {
                                                modSuccess = false;
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ****************************************************");
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: Mod cache was not found, mod was not updated.");
                                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ****************************************************");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            modSuccess = false;
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***********************");
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: Failed mod copy.\r\n{ex.Message}");
                                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***********************");
                                        }
                                    }
                                }

                                if (!modSuccess)
                                {
                                    success = false;
                                    failedMods.Add($"{index + 1} of {modIdList.Count} - {modTitle}");
                                }

                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Finished processing mod {modId}.\r\n");
                            }

                            if (failedMods.Count > 0)
                            {
                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} **************************************************************************");
                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: The following mods failed the update, check above for more details.");
                                foreach (var failedMod in failedMods)
                                    progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} {failedMod}");
                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} **************************************************************************\r\n");
                            }
                        }
                        else
                        {
                            success = false;
                            // no steam information downloaded, display an error
                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ********************************************************************");
                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: Mods cannot be updated, unable to download steam information.");
                            progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ********************************************************************\r\n");

                            if (!Config.Default.ServerUpdate_ForceUpdateModsIfNoSteamInfo)
                                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} If the mod update keeps failing try enabling the '{_globalizer.GetResourceString("GlobalSettings_ForceUpdateModsIfNoSteamInfoLabel")}' option in the settings window.\r\n");
                        }
                    }
                }
                else
                {
                    if (updateServer && updateMods)
                    {
                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***********************************************************");
                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ERROR: Mods were not processed as server update had errors.");
                        progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} ***********************************************************\r\n");
                    }
                }

                progressCallback?.Invoke(0, $"{SteamCmdUpdater.OUTPUT_PREFIX} Finished upgrade process.");
                return success;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            finally
            {
                this.lastModStatusQuery = DateTime.MinValue;
                UpdateServerStatus(ServerStatus.Stopped, false);
            }
        }


        private void CheckServerWorldFileExists()
        {
            var serverApp = new ServerApp()
            {
                BackupWorldFile = false,
                DeleteOldBackupFiles = false,
                SendAlerts = false,
                SendEmails = false,
                OutputLogs = false
            };
            serverApp.CheckServerWorldFileExists(ProfileSnapshot);
        }

        public void DeleteFirewallRules()
        {
            if (Config.Default.ManageFirewallAutomatically)
            {
                if (SecurityUtils.IsAdministrator())
                {
                    var serverExe = GetServerExe();

                    FirewallUtils.DeleteFirewallRules(serverExe);
                }
            }
        }

        public void ResetModCheckTimer()
        {
            this.lastModStatusQuery = DateTime.MinValue;
        }

        public void UpdateServerStatus(ServerStatus serverStatus, bool sendAlert)
        {
            var availability = Availability;

            switch (serverStatus)
            {
                case ServerStatus.Stopped:
                case ServerStatus.Stopping:
                case ServerStatus.Uninstalled:
                case ServerStatus.Updating:
                    availability = AvailabilityStatus.Unavailable;
                    break;
                case ServerStatus.Unknown:
                    availability = AvailabilityStatus.Unknown;
                    sendAlert = false;
                    break;
            }

            UpdateServerStatus(serverStatus, availability, sendAlert);
        }

        public void UpdateServerStatus(ServerStatus serverStatus, AvailabilityStatus availabilityStatus, bool sendAlert)
        {
            this.Status = serverStatus;
            this.Availability = availabilityStatus;

            UpdateServerStatusString();

            if (!string.IsNullOrWhiteSpace(Config.Default.Alert_ServerStatusChange) && sendAlert)
                PluginHelper.Instance.ProcessAlert(AlertType.ServerStatusChange, this.ProfileSnapshot.ProfileName, $"{Config.Default.Alert_ServerStatusChange} {StatusString}");
        }

        public void UpdateServerStatusString()
        {
            StatusString = GetServerStatusString(Status);
        }

        public static string GetServerStatusString(ServerStatus status)
        {
            switch (status)
            {
                case ServerStatus.Initializing:
                    return _globalizer.GetResourceString("ServerSettings_RuntimeStatusInitializingLabel");
                case ServerStatus.Running:
                    return _globalizer.GetResourceString("ServerSettings_RuntimeStatusRunningLabel");
                case ServerStatus.Stopped:
                    return _globalizer.GetResourceString("ServerSettings_RuntimeStatusStoppedLabel");
                case ServerStatus.Stopping:
                    return _globalizer.GetResourceString("ServerSettings_RuntimeStatusStoppingLabel");
                case ServerStatus.Uninstalled:
                    return _globalizer.GetResourceString("ServerSettings_RuntimeStatusUninstalledLabel");
                case ServerStatus.Unknown:
                    return _globalizer.GetResourceString("ServerSettings_RuntimeStatusUnknownLabel");
                case ServerStatus.Updating:
                    return _globalizer.GetResourceString("ServerSettings_RuntimeStatusUpdatingLabel");
                default:
                    return _globalizer.GetResourceString("ServerSettings_RuntimeStatusUnknownLabel");
            }
        }

        public void DisableMotDIntervalTimer()
        {
            var snapshot = this.ProfileSnapshot;
            snapshot.MOTDIntervalEnabled = false;
            this.ProfileSnapshot = snapshot;

            this.motdIntervalTimer.Stop();
        }

        private async Task HandleMotDIntervalTimer()
        {
            await TaskUtils.RunOnUIThreadAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(this.ProfileSnapshot.MOTD))
                    return;

                if (this.ProfileSnapshot.RCONEnabled)
                {
                    try
                    {
                        await SendMessageAsync(this.ProfileSnapshot.MOTD, CancellationToken.None);
                        _logger.Debug($"Message of the Day shown.");
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        CloseRconConsole();
                    }
                }

                await Task.Delay(1);
            });
        }

        private void CloseRconConsole()
        {
            if (_rconConsole != null)
            {
                _rconConsole.Dispose();
                _rconConsole = null;

                Task.Delay(1000).Wait();
            }
        }

        private void SetupRconConsole()
        {
            CloseRconConsole();

            if (this.ProfileSnapshot == null || !this.ProfileSnapshot.RCONEnabled)
                return;

            try
            {
                var endPoint = new IPEndPoint(this.ProfileSnapshot.ServerIPAddress, this.ProfileSnapshot.RCONPort);
                var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endPoint, sendTimeOut: 10000, receiveTimeOut: 10000);

                if (server == null)
                {
                    return;
                }

                Task.Delay(1000).Wait();

                _rconConsole = server.GetControl(this.ProfileSnapshot.RCONPassword);
            }
            catch (Exception)
            {
                _rconConsole = null;
            }
        }

        private async Task<bool> SendCommandAsync(string command, bool retryIfFailed)
        {
            if (this.ProfileSnapshot == null || !this.ProfileSnapshot.RCONEnabled)
                return false;
            if (string.IsNullOrWhiteSpace(command))
                return false;

            int retries = 0;
            int rconRetries = 0;
            int maxRetries = retryIfFailed ? RCON_MAXRETRIES : 1;

            while (retries < maxRetries && rconRetries < RCON_MAXRETRIES)
            {
                SetupRconConsole();
    
                if (_rconConsole == null)
                {
                    rconRetries++;
                }
                else
                {
                    rconRetries = 0;

                    try
                    {
                        _rconConsole.SendCommand(command);

                        return true;
                    }
                    catch (Exception)
                    {
                    }

                    retries++;
                }
            }

            return false;
        }

        private async Task<bool> SendMessageAsync(string message, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            var sent = await SendCommandAsync($"{Config.Default.RCON_MessageCommand.ToLower()} {message}", false);

            if (sent)
            {
                try
                {
                    Task.Delay(Config.Default.SendMessageDelay, token).Wait(token);
                }
                catch { }
            }

            return sent;
        }
    }
}
