using ServerManagerTool.Common.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public SettingsWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);
        }

        private void SettingsWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!globalSettingsControl.IsEnabled)
                e.Cancel = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            globalSettingsControl.CloseControl();
            globalSettingsControl.ApplyChangesToConfig();

            if (SecurityUtils.IsAdministrator())
            {
                // check if the Auto Update has been enabled.
                if (Config.Default.AutoUpdate_EnableUpdate)
                {
                    // check if an update period has been set.
                    if (Config.Default.AutoUpdate_UpdatePeriod <= 0)
                    {
                        MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_CacheUpdate_DisabledLabel"), _globalizer.GetResourceString("GlobalSettings_CacheUpdate_DisabledTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    // check if the cache directory has been set and it exists.
                    if (string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_CacheDir) || !Directory.Exists(Config.Default.AutoUpdate_CacheDir))
                    {
                        MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_CacheDirectory_ErrorLabel"), _globalizer.GetResourceString("GlobalSettings_CacheDirectory_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var taskKey = TaskSchedulerUtils.ComputeKey(Config.Default.DataPath);

                var command = Assembly.GetEntryAssembly().Location;
                if (!TaskSchedulerUtils.ScheduleAutoUpdate(taskKey, null, command, Config.Default.AutoUpdate_EnableUpdate ? Config.Default.AutoUpdate_UpdatePeriod : 0, Config.Default.AutoUpdate_TaskPriority))
                {
                    MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_CacheTaskUpdate_ErrorLabel"), _globalizer.GetResourceString("GlobalSettings_CacheTaskUpdate_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (Config.Default.AutoUpdate_EnableUpdate && Config.Default.AutoUpdate_UpdatePeriod > 0)
                    {
                        MessageBox.Show(String.Format(_globalizer.GetResourceString("GlobalSettings_CacheUpdate_EnabledLabel"), Config.Default.AutoUpdate_UpdatePeriod), _globalizer.GetResourceString("GlobalSettings_CacheUpdate_EnabledTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_CacheUpdate_DisabledLabel"), _globalizer.GetResourceString("GlobalSettings_CacheUpdate_DisabledTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                if (!TaskSchedulerUtils.ScheduleAutoBackup(taskKey, null, command, Config.Default.AutoBackup_EnableBackup ? Config.Default.AutoBackup_BackupPeriod : 0, Config.Default.AutoBackup_TaskPriority))
                {
                    MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_BackupTaskUpdate_ErrorLabel"), _globalizer.GetResourceString("GlobalSettings_BackupTaskUpdate_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (Config.Default.AutoBackup_EnableBackup && Config.Default.AutoBackup_BackupPeriod > 0)
                    {
                        MessageBox.Show(String.Format(_globalizer.GetResourceString("GlobalSettings_BackupTaskUpdate_EnabledLabel"), Config.Default.AutoBackup_BackupPeriod), _globalizer.GetResourceString("GlobalSettings_BackupTaskUpdate_EnabledTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_BackupTaskUpdate_DisabledLabel"), _globalizer.GetResourceString("GlobalSettings_BackupTaskUpdate_DisabledTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            if (Config.Default.SteamCmdRedirectOutput && !Config.Default.SteamCmd_UseAnonymousCredentials)
            {
                MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_SteamCMDAuthentication_DisabledLabel"), _globalizer.GetResourceString("GlobalSettings_SteamCMDAuthentication_DisabledTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                Config.Default.SteamCmd_UseAnonymousCredentials = true;
            }

            App.SaveConfigFiles(false);

            base.OnClosed(e);
        }
    }
}
