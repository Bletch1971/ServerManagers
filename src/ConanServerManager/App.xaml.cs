using NLog;
using NLog.Config;
using NLog.Targets;
using ServerManagerTool.Common;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.DiscordBot;
using ServerManagerTool.DiscordBot.Models;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using ServerManagerTool.Plugin.Common;
using ServerManagerTool.Utils;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

        private CancellationTokenSource _tokenSourceDiscordBot;
        private GlobalizedApplication _globalizer;
        private bool _applicationStarted;
        private string _args;
        private bool _betaVersion;
        private string _title;
        private string _version;

        public App()
        {
            if (string.IsNullOrWhiteSpace(Config.Default.ServerManagerUniqueKey))
            {
                Config.Default.ServerManagerUniqueKey = Guid.NewGuid().ToString();
            }

            if (!string.IsNullOrWhiteSpace(Config.Default.DataPath))
            {
                var root = Path.GetPathRoot(Config.Default.DataPath);
                if (!root.EndsWith("\\"))
                {
                    Config.Default.DataPath = Config.Default.DataPath.Replace(root, root + "\\");
                }
            }

            if (!string.IsNullOrWhiteSpace(Config.Default.ConfigPath))
            {
                var root = Path.GetPathRoot(Config.Default.ConfigPath);
                if (!root.EndsWith("\\"))
                {
                    Config.Default.ConfigPath = Config.Default.ConfigPath.Replace(root, root + "\\");
                }
            }

            if (!string.IsNullOrWhiteSpace(Config.Default.BackupPath))
            {
                var root = Path.GetPathRoot(Config.Default.BackupPath);
                if (!root.EndsWith("\\"))
                {
                    Config.Default.BackupPath = Config.Default.BackupPath.Replace(root, root + "\\");
                }
            }

            if (!string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_CacheDir))
            {
                var root = Path.GetPathRoot(Config.Default.AutoUpdate_CacheDir);
                if (!root.EndsWith("\\"))
                {
                    Config.Default.AutoUpdate_CacheDir = Config.Default.AutoUpdate_CacheDir.Replace(root, root + "\\");
                }
            }

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

        public bool DiscordBotStarted
        {
            get
            {
                return _tokenSourceDiscordBot != null;
            }
            set
            {
                OnPropertyChanged();
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

        public static string GetLogFolder() => IOUtils.NormalizePath(Path.Combine(Config.Default.DataPath, Config.Default.LogsRelativePath));

        public static string GetProfileLogFolder(string profileId) => IOUtils.NormalizePath(Path.Combine(Config.Default.DataPath, Config.Default.LogsRelativePath, profileId.ToLower()));

        public static Logger GetProfileLogger(string profileId, string logName, LogLevel minLevel, LogLevel maxLevel)
        {
            if (string.IsNullOrWhiteSpace(profileId) || string.IsNullOrWhiteSpace(logName))
                return null;

            var loggerName = $"{profileId.ToLower()}_{logName}".Replace(" ", "_");

            if (LogManager.Configuration.FindTargetByName(loggerName) == null)
            {
                var logFilePath = GetProfileLogFolder(profileId);

                var logFile = new FileTarget(loggerName)
                {
                    FileName = Path.Combine(logFilePath, $"{logName}.log"),
                    Layout = "${time} [${level:uppercase=true}] ${message}",
                    ArchiveFileName = Path.Combine(logFilePath, $"{logName}.{{#}}.log"),
                    ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                    ArchiveEvery = FileArchivePeriod.Day,
                    ArchiveDateFormat = "yyyyMMdd",
                    ArchiveOldFileOnStartup = true,
                    MaxArchiveFiles = Config.Default.LoggingMaxArchiveFiles,
                    MaxArchiveDays = Config.Default.LoggingMaxArchiveDays,
                    CreateDirs = true,
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
            }
            if (!Config.Default.DiscordBotPrefixFixed)
            {
                if (!Config.Default.DiscordBotPrefix.EndsWith("!"))
                    Config.Default.DiscordBotPrefix += "!";
                Config.Default.DiscordBotPrefixFixed = true;
                Config.Default.Save();
                Config.Default.Reload();
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
            PluginHelper.Instance.SetFetchProfileCallback(DiscordPluginHelper.FetchProfiles);
            OnResourceDictionaryChanged(Thread.CurrentThread.CurrentCulture.Name);

            // check if we are starting server manager for server shutdown
            if (e.Args.Any(a => a.StartsWith(Constants.ARG_AUTOSHUTDOWN1, StringComparison.OrdinalIgnoreCase)))
            {
                var arg = e.Args.FirstOrDefault(a => a.StartsWith(Constants.ARG_AUTOSHUTDOWN1, StringComparison.OrdinalIgnoreCase));
                var exitCode = ServerApp.PerformAutoShutdown(arg, ServerProcessType.AutoShutdown1);

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting server manager for server shutdown
            if (e.Args.Any(a => a.StartsWith(Constants.ARG_AUTOSHUTDOWN2, StringComparison.OrdinalIgnoreCase)))
            {
                var arg = e.Args.FirstOrDefault(a => a.StartsWith(Constants.ARG_AUTOSHUTDOWN2, StringComparison.OrdinalIgnoreCase));
                var exitCode = ServerApp.PerformAutoShutdown(arg, ServerProcessType.AutoShutdown2);

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting server manager for server updating
            if (e.Args.Any(a => a.Equals(Constants.ARG_AUTOUPDATE, StringComparison.OrdinalIgnoreCase)))
            {
                var exitCode = ServerApp.PerformAutoUpdate();

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting server manager for server backups
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

            var restartRequired = false;
            if (string.IsNullOrWhiteSpace(Config.Default.DataPath))
            {
                var dataDirectoryWindow = new DataDirectoryWindow();
                dataDirectoryWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                var result = dataDirectoryWindow.ShowDialog();

                if (!result.HasValue || !result.Value)
                {
                    Environment.Exit(0);
                }

                restartRequired = true;
            }

            Config.Default.ConfigPath = Path.Combine(Config.Default.DataPath, Config.Default.ProfilesRelativePath);
            System.IO.Directory.CreateDirectory(Config.Default.ConfigPath);
            SaveConfigFiles();

            if (restartRequired)
            {
                Environment.Exit(0);
            }

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

            if (Config.Default.DiscordBotEnabled)
            {
                StartDiscordBot();
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

        public void OnResourceDictionaryChanged(string languageCode)
        {
            PluginHelper.Instance.OnResourceDictionaryChanged(languageCode);
        }

        public static void ReconfigureLogging()
        {
            UpdateLoggingStatus();

            var logDir = Path.Combine(Config.Default.DataPath, Config.Default.LogsRelativePath);
            if (!System.IO.Directory.Exists(logDir))
                System.IO.Directory.CreateDirectory(logDir);

            LogManager.Configuration.Variables["logDir"] = logDir;

            var fileTargets = LogManager.Configuration.AllTargets.OfType<FileTarget>();
            foreach (var fileTarget in fileTargets)
            {
                var fileName = Path.GetFileNameWithoutExtension(fileTarget.FileName.ToString());
                fileTarget.FileName = Path.Combine(logDir, $"{fileName}.log");
                fileTarget.ArchiveFileName = Path.Combine(logDir, $"{fileName}.{{#}}.log");
                fileTarget.MaxArchiveFiles = Config.Default.LoggingMaxArchiveFiles;
                fileTarget.MaxArchiveDays = Config.Default.LoggingMaxArchiveDays;
                fileTarget.CreateDirs = true;
            }

            LogManager.ReconfigExistingLoggers();
        }

        public static void SaveConfigFiles(bool includeBackup = true)
        {
            Config.Default.Save();
            CommonConfig.Default.Save();

            Config.Default.Reload();
            CommonConfig.Default.Reload();

            var installFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var backupFolder = includeBackup
                ? string.IsNullOrWhiteSpace(Config.Default.BackupPath)
                        ? Path.Combine(Config.Default.DataPath, Config.Default.BackupRelativePath)
                        : Path.Combine(Config.Default.BackupPath)
                : null;

            SettingsUtils.BackupUserConfigSettings(Config.Default, "userconfig.json", installFolder, backupFolder);
            SettingsUtils.BackupUserConfigSettings(CommonConfig.Default, "commonconfig.json", installFolder, backupFolder);
        }

        private void ShutDownApplication()
        {
            StopDiscordBot();

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
                SaveConfigFiles();
            }

            PluginHelper.Instance?.Dispose();
            LogManager.Flush();
            LogManager.Shutdown();

            ApplicationStarted = false;
        }

        public void StartDiscordBot()
        {
            if (_tokenSourceDiscordBot != null)
            {
                return;
            }

            _tokenSourceDiscordBot = new CancellationTokenSource();
            DiscordBotStarted = true;

            Task discordTask = Task.Run(async () =>
            {
                var config = new DiscordBotConfig
                {
                    LogLevel = Config.Default.DiscordBotLogLevel,
                    MaxArchiveFiles = Config.Default.LoggingMaxArchiveFiles,
                    MaxArchiveDays = Config.Default.LoggingMaxArchiveDays,
                    DiscordToken = Config.Default.DiscordBotToken,
                    CommandPrefix = Config.Default.DiscordBotPrefix,
                    DataDirectory = Config.Default.DataPath,
                    AllowAllBots = Config.Default.DiscordBotAllowAllBots,
                };
                if (Config.Default.DiscordBotWhitelist != null)
                {
                    config.DiscordBotWhitelists = Config.Default.DiscordBotWhitelist.Cast<string>();
                }

                await ServerManagerBotFactory.GetServerManagerBot().RunAsync(config, DiscordBotHelper.HandleDiscordCommand, DiscordBotHelper.HandleTranslation, _tokenSourceDiscordBot.Token);

                if (_tokenSourceDiscordBot != null)
                {
                    // cleanup the token
                    _tokenSourceDiscordBot.Dispose();
                    _tokenSourceDiscordBot = null;
                }
                DiscordBotStarted = false;
            }, _tokenSourceDiscordBot.Token)
                .ContinueWith(t => {
                    var message = t.Exception.InnerException is null ? t.Exception.Message : t.Exception.InnerException.Message;
                    if (message.StartsWith("#"))
                    {
                        message = _globalizer.GetResourceString(message.Substring(1)) ?? message.Substring(1);
                    }

                    MessageBox.Show(message, _globalizer.GetResourceString("DiscordBot_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);

                    if (_tokenSourceDiscordBot != null)
                    {
                        // cleanup the token
                        _tokenSourceDiscordBot.Dispose();
                        _tokenSourceDiscordBot = null;
                    }
                    DiscordBotStarted = false;
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void StopDiscordBot()
        {
            if (!(_tokenSourceDiscordBot is null))
            {
                _tokenSourceDiscordBot.Cancel();
            }
        }

        public static void UpdateLoggingStatus()
        {
            if (Config.Default.LoggingEnabled)
            {
                while (!LogManager.IsLoggingEnabled())
                    LogManager.EnableLogging();
            }
            else
            {
                while (LogManager.IsLoggingEnabled())
                    LogManager.DisableLogging();
            }

            Debug.WriteLine($"Logging Enabled: {LogManager.IsLoggingEnabled()}");
        }
    }
}
