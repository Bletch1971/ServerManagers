using ArkData;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using NLog.Config;
using NLog.Targets;
using ServerManagerTool.Common;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using ServerManagerTool.Plugin.Common;
using ServerManagerTool.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : GlobalizedApplication, INotifyPropertyChanged
    {
        public new static App Instance
        {
            get;
            private set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private GlobalizedApplication _globalizer;
        private bool _applicationStarted;
        private string _args;
        private bool _betaVersion;
        private string _title;
        private string _version;

        public App()
        {
            if (string.IsNullOrWhiteSpace(Config.Default.ASMUniqueKey))
                Config.Default.ASMUniqueKey = Guid.NewGuid().ToString();

            App.Instance = this;
            ApplicationStarted = false;
            Args = string.Empty;
            BetaVersion = false;
            Title = string.Empty;
            Version = AppUtils.GetDeployedVersion(Assembly.GetEntryAssembly());

            AppDomain.CurrentDomain.UnhandledException += ErrorHandling.CurrentDomain_UnhandledException;

            MigrateSettings();
            ReconfigureLogging();
        }

        public bool ApplicationStarted
        {
            get
            {
                return _applicationStarted;
            }
            set
            {
                if (!Equals(value, _applicationStarted))
                {
                    _applicationStarted = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Args
        {
            get
            {
                return _args;
            }
            set
            {
                if (!Equals(value, _args))
                {
                    _args = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool BetaVersion
        {
            get
            {
                return _betaVersion;
            }
            set
            {
                if (!Equals(value, _betaVersion))
                {
                    _betaVersion = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (!Equals(value, _title))
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                if (!Equals(value, _version))
                {
                    _version = value;
                    OnPropertyChanged();
                }
            }
        }

        public static void DiscoverMachinePublicIP(bool forceOverride)
        {
            if (forceOverride || string.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
            {
                var publicIP = NetworkUtils.DiscoverPublicIP();
                if (string.IsNullOrWhiteSpace(publicIP))
                    return;

                if (!Config.Default.MachinePublicIP.Equals(publicIP, StringComparison.OrdinalIgnoreCase))
                {
                    Config.Default.MachinePublicIP = publicIP;
                }
            }
        }

        public static async Task DiscoverMachinePublicIPAsync(bool forceOverride)
        {
            if (forceOverride || string.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
            {
                var publicIP = await NetworkUtils.DiscoverPublicIPAsync();
                if (string.IsNullOrWhiteSpace(publicIP))
                    return;

                if (!Config.Default.MachinePublicIP.Equals(publicIP, StringComparison.OrdinalIgnoreCase))
                {
                    await App.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Config.Default.MachinePublicIP = publicIP;
                    }));
                }
            }
        }

        private IList<Plugin.Common.Lib.Profile> FetchProfiles()
        {
            return ServerManager.Instance.Servers.Select(s => new ServerManagerTool.Plugin.Common.Lib.Profile() { ProfileName = s?.Profile?.ProfileName ?? string.Empty, InstallationFolder = s?.Profile?.InstallDirectory ?? string.Empty }).ToList();
        }

        public static string GetLogFolder() => IOUtils.NormalizePath(Path.Combine(Config.Default.DataDir, Config.Default.LogsDir));

        public static string GetProfileLogFolder(string profileId) => IOUtils.NormalizePath(Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, profileId.ToLower()));

        public static Logger GetProfileLogger(string profileId, string name, LogLevel minLevel, LogLevel maxLevel)
        {
            if (string.IsNullOrWhiteSpace(profileId) || string.IsNullOrWhiteSpace(name))
                return null;

            var loggerName = $"{profileId.ToLower()}_{name}".Replace(" ", "_");

            if (LogManager.Configuration.FindTargetByName(loggerName) == null)
            {            
                var logFilePath = GetProfileLogFolder(profileId);
                if (!System.IO.Directory.Exists(logFilePath))
                    System.IO.Directory.CreateDirectory(logFilePath);

                var logFile = new FileTarget(loggerName)
                {
                    FileName = Path.Combine(logFilePath, $"{name}.log"),
                    Layout = "${time} ${message}",
                    ArchiveFileName = Path.Combine(logFilePath, $"{name}.{{#}}.log"),
                    ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                    ArchiveEvery = FileArchivePeriod.Day,
                    ArchiveDateFormat = "yyyyMMdd"
                };
                LogManager.Configuration.AddTarget(loggerName, logFile);

                var rule = new LoggingRule(loggerName, minLevel, maxLevel, logFile);
                LogManager.Configuration.LoggingRules.Add(rule);
                LogManager.ReconfigExistingLoggers();
            }

            return LogManager.GetLogger(loggerName);
        }

        private static void MigrateSettings()
        {
            var installFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //
            // Migrate settings when we update.
            //
            if (CommonConfig.Default.UpgradeConfig)
            {
                var settingsFile = IOUtils.NormalizePath(Path.Combine(installFolder, "commonconfig.json"));

                CommonConfig.Default.Upgrade();
                CommonConfig.Default.Reload();
                SettingsUtils.MigrateSettings(CommonConfig.Default, settingsFile);
                CommonConfig.Default.UpgradeConfig = false;
                CommonConfig.Default.Save();
            }
            if (Config.Default.UpgradeConfig)
            {
                var settingsFile = IOUtils.NormalizePath(Path.Combine(installFolder, "userconfig.json"));

                Config.Default.Upgrade();
                Config.Default.Reload();
                SettingsUtils.MigrateSettings(Config.Default, settingsFile);
                Config.Default.UpgradeConfig = false;
                Config.Default.Save();

                if (string.IsNullOrWhiteSpace(CommonConfig.Default.SteamAPIKey))
                {
                    CommonConfig.Default.SteamAPIKey = Config.Default.SteamAPIKey;
                    CommonConfig.Default.Save();
                }
            }

            Config.Default.SteamCmdRedirectOutput = false;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            _globalizer = GlobalizedApplication.Instance;
            try
            {
                if (!string.IsNullOrWhiteSpace(Config.Default.CultureName))
                    _globalizer.GlobalizationManager.SwitchLanguage(Config.Default.CultureName, true);
            }
            catch (Exception ex)
            {
                // just output the exception message, it should default back to the fallback language.
                Debug.WriteLine(ex.Message);
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(Config.Default.StyleName))
                    _globalizer.StyleManager.SwitchStyle(Config.Default.StyleName, true);
            }
            catch (Exception ex)
            {
                // just output the exception message, it should default back to the fallback style.
                Debug.WriteLine(ex.Message);
            }

            TaskSchedulerUtils.TaskFolder = Config.Default.ScheduledTaskFolder;

            Args = string.Join(" ", e.Args);

            // check if we are starting server manager in BETA/TEST mode
            if (e.Args.Any(a => a.Equals(Constants.ARG_BETA, StringComparison.OrdinalIgnoreCase) || a.Equals(Constants.ARG_TEST, StringComparison.OrdinalIgnoreCase)))
            {
                BetaVersion = true;
            }

            // check if we need to set the title
            if (e.Args.Any(a => a.Equals(Constants.ARG_TITLE, StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < e.Args.Length - 1; i++)
                {
                    if (e.Args[i].Equals(Constants.ARG_TITLE, StringComparison.OrdinalIgnoreCase) && i < e.Args.Length - 1 && !e.Args[i + 1].StartsWith("-"))
                    {
                        Title = e.Args[i + 1].Trim();
                    }
                }
            }

            // check and update the public IP address
            DiscoverMachinePublicIP(Config.Default.ManagePublicIPAutomatically);

            var installPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            PluginHelper.Instance.BetaEnabled = this.BetaVersion;
            PluginHelper.Instance.LoadPlugins(installPath, true);
            PluginHelper.Instance.SetFetchProfileCallback(FetchProfiles);

            // check if we are starting ASM for the old server restart - no longer supported
            if (e.Args.Any(a => a.StartsWith(Constants.ARG_AUTORESTART, StringComparison.OrdinalIgnoreCase)))
            {
                // just exit
                Environment.Exit(0);
            }

            // check if we are starting ASM for server shutdown
            if (e.Args.Any(a => a.StartsWith(Constants.ARG_AUTOSHUTDOWN1, StringComparison.OrdinalIgnoreCase)))
            {
                var arg = e.Args.FirstOrDefault(a => a.StartsWith(Constants.ARG_AUTOSHUTDOWN1, StringComparison.OrdinalIgnoreCase));
                var exitCode = ServerApp.PerformAutoShutdown(arg, ServerProcessType.AutoShutdown1);

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting ASM for server shutdown
            if (e.Args.Any(a => a.StartsWith(Constants.ARG_AUTOSHUTDOWN2, StringComparison.OrdinalIgnoreCase)))
            {
                var arg = e.Args.FirstOrDefault(a => a.StartsWith(Constants.ARG_AUTOSHUTDOWN2, StringComparison.OrdinalIgnoreCase));
                var exitCode = ServerApp.PerformAutoShutdown(arg, ServerProcessType.AutoShutdown2);

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting ASM for server updating
            if (e.Args.Any(a => a.Equals(Constants.ARG_AUTOUPDATE, StringComparison.OrdinalIgnoreCase)))
            {
                var exitCode = ServerApp.PerformAutoUpdate();

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting ASM for server backups
            if (e.Args.Any(a => a.Equals(Constants.ARG_AUTOBACKUP, StringComparison.OrdinalIgnoreCase)))
            {
                var exitCode = ServerApp.PerformAutoBackup();

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            if (Config.Default.RunAsAdministratorPrompt && !SecurityUtils.IsAdministrator())
            {
                var result = MessageBox.Show(_globalizer.GetResourceString("Application_RunAsAdministratorLabel"), _globalizer.GetResourceString("Application_RunAsAdministratorTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var processInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().CodeBase)
                    {

                        // The following properties run the new process as administrator
                        UseShellExecute = true,
                        Verb = "runas",
                        Arguments = string.Join(" ", e.Args)
                    };

                    // Start the new process
                    try
                    {
                        Process.Start(processInfo);

                        // Shut down the current process
                        Application.Current.Shutdown(0);

                        return;
                    }
                    catch (Exception)
                    {
                        // The user did not allow the application to run as administrator
                        MessageBox.Show(_globalizer.GetResourceString("Application_RunAsAdministrator_FailedLabel"), _globalizer.GetResourceString("Application_RunAsAdministrator_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            // check if application is already running
            if (ProcessUtils.IsAlreadyRunning())
            {
                var result = MessageBox.Show(_globalizer.GetResourceString("Application_SingleInstanceLabel"), _globalizer.GetResourceString("Application_SingleInstanceTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (ProcessUtils.SwitchToCurrentInstance())
                    {
                        // Shut down the current process
                        Application.Current.Shutdown(0);

                        return;
                    }

                    MessageBox.Show(_globalizer.GetResourceString("Application_SingleInstance_FailedLabel"), _globalizer.GetResourceString("Application_SingleInstance_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            ApplicationStarted = true;

            if (string.IsNullOrWhiteSpace(Config.Default.DataDir))
            {              
                var dataDirectoryWindow = new DataDirectoryWindow();
                dataDirectoryWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                var result = dataDirectoryWindow.ShowDialog();

                if (!result.HasValue || !result.Value)
                {
                    Environment.Exit(0);
                }
            }

            Config.Default.ConfigDirectory = Path.Combine(Config.Default.DataDir, Config.Default.ProfilesDir);            
            System.IO.Directory.CreateDirectory(Config.Default.ConfigDirectory);
            Config.Default.Save();
            CommonConfig.Default.Save();

            DataFileDetails.PlayerFileExtension = Config.Default.PlayerFileExtension;
            DataFileDetails.TribeFileExtension = Config.Default.TribeFileExtension;

            if (e.Args.Any(a => a.StartsWith(Constants.ARG_SERVERMONITOR, StringComparison.OrdinalIgnoreCase)))
            {
                ServerRuntime.EnableUpdateModStatus = false;
                ServerProfile.EnableServerFilesWatcher = false;

                StartupUri = new Uri("Windows/ServerMonitorWindow.xaml", UriKind.RelativeOrAbsolute);
            }
            else
            {
                // initialize all the game data
                GameData.Initialize();

                StartupUri = new Uri("Windows/AutoUpdateWindow.xaml", UriKind.RelativeOrAbsolute);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ShutDownApplication();

            base.OnExit(e);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static void ReconfigureLogging()
        {               
            string logDir = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir);
            if (!System.IO.Directory.Exists(logDir))
                System.IO.Directory.CreateDirectory(logDir);

            LogManager.Configuration.Variables["logDir"] = logDir;

            var fileTargets = LogManager.Configuration.AllTargets.OfType<FileTarget>();
            foreach (var fileTarget in fileTargets)
            {
                var fileName = Path.GetFileNameWithoutExtension(fileTarget.FileName.ToString());
                fileTarget.FileName = Path.Combine(logDir, $"{fileName}.log");
                fileTarget.ArchiveFileName = Path.Combine(logDir, $"{fileName}.{{#}}.log");
            }

            LogManager.ReconfigExistingLoggers();
        }   

        private void ShutDownApplication()
        {
            if (ApplicationStarted)
            {
                foreach (var server in ServerManager.Instance.Servers)
                {
                    try
                    {
                        server.Profile.Save(false, false, null);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(String.Format(_globalizer.GetResourceString("Application_Profile_SaveFailedLabel"), server.Profile.ProfileName, ex.Message, ex.StackTrace), _globalizer.GetResourceString("Application_Profile_SaveFailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                Config.Default.Save();
                CommonConfig.Default.Save();
            }

            PluginHelper.Instance?.Dispose();

            ApplicationStarted = false;
        }
    }
}
