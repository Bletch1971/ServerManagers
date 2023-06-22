using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using ServerManagerTool.Common;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for GlobalSettingsControl.xaml
    /// </summary>
    public partial class GlobalSettingsControl : UserControl
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty AppInstanceProperty = DependencyProperty.Register(nameof(AppInstance), typeof(App), typeof(GlobalSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty IsAdministratorProperty = DependencyProperty.Register(nameof(IsAdministrator), typeof(bool), typeof(GlobalSettingsControl), new PropertyMetadata(false));
        public static readonly DependencyProperty WindowStatesMainWindowProperty = DependencyProperty.Register(nameof(WindowStatesMainWindow), typeof(ComboBoxItemList), typeof(GlobalSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty WindowStatesServerMonitorProperty = DependencyProperty.Register(nameof(WindowStatesServerMonitor), typeof(ComboBoxItemList), typeof(GlobalSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty DiscordBotLogLevelsProperty = DependencyProperty.Register(nameof(DiscordBotLogLevels), typeof(ComboBoxItemList), typeof(GlobalSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty DiscordBotWhitelistProperty = DependencyProperty.Register(nameof(DiscordBotWhitelist), typeof(List<DiscordBotWhitelistItem>), typeof(GlobalSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty RconMessageModesProperty = DependencyProperty.Register(nameof(RconMessageModes), typeof(ComboBoxItemList), typeof(GlobalSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty TaskPrioritiesProperty = DependencyProperty.Register(nameof(TaskPriorities), typeof(ComboBoxItemList), typeof(GlobalSettingsControl), new PropertyMetadata(null));

        public GlobalSettingsControl()
        {
            this.AppInstance = App.Instance;
            this.Config = Config.Default;
            this.CommonConfig = CommonConfig.Default;
            this.IsAdministrator = SecurityUtils.IsAdministrator();
            this.Version = GetDeployedVersion();

            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            PopulateWindowsStatesMainWindowComboBox();
            PopulateWindowsStatesServerMonitorWindowComboBox();
            PopulateDiscordBotLogLevelsComboBox();
            PopulateRconMessageModesComboBox();
            PopulateTaskPrioritiesComboBox();

            DiscordBotWhitelist = new List<DiscordBotWhitelistItem>();
            if (Config.DiscordBotWhitelist != null)
            {
                DiscordBotWhitelist.AddRange(Config.DiscordBotWhitelist.Select(i => new DiscordBotWhitelistItem() { BotId = i }));
            }

            this.DataContext = this;

            GameData.GameDataLoaded += GameData_GameDataLoaded;
        }

        public App AppInstance
        {
            get { return GetValue(AppInstanceProperty) as App; }
            set { SetValue(AppInstanceProperty, value); }
        }

        public Config Config
        {
            get;
            set;
        }

        public CommonConfig CommonConfig
        {
            get;
            set;
        }

        public bool IsAdministrator
        {
            get { return (bool)GetValue(IsAdministratorProperty); }
            set { SetValue(IsAdministratorProperty, value); }
        }

        public ComboBoxItemList WindowStatesMainWindow
        {
            get { return (ComboBoxItemList)GetValue(WindowStatesMainWindowProperty); }
            set { SetValue(WindowStatesMainWindowProperty, value); }
        }

        public ComboBoxItemList WindowStatesServerMonitor
        {
            get { return (ComboBoxItemList)GetValue(WindowStatesServerMonitorProperty); }
            set { SetValue(WindowStatesServerMonitorProperty, value); }
        }

        public string Version
        {
            get;
            set;
        }

        public ComboBoxItemList DiscordBotLogLevels
        {
            get { return (ComboBoxItemList)GetValue(DiscordBotLogLevelsProperty); }
            set { SetValue(DiscordBotLogLevelsProperty, value); }
        }

        public List<DiscordBotWhitelistItem> DiscordBotWhitelist
        {
            get { return (List<DiscordBotWhitelistItem>)GetValue(DiscordBotWhitelistProperty); }
            set { SetValue(DiscordBotWhitelistProperty, value); }
        }

        public ComboBoxItemList RconMessageModes
        {
            get { return (ComboBoxItemList)GetValue(RconMessageModesProperty); }
            set { SetValue(RconMessageModesProperty, value); }
        }

        public ComboBoxItemList TaskPriorities
        {
            get { return (ComboBoxItemList)GetValue(TaskPrioritiesProperty); }
            set { SetValue(TaskPrioritiesProperty, value); }
        }

        public void ApplyChangesToConfig()
        {
            if (Config.DiscordBotWhitelist is null)
            {
                Config.DiscordBotWhitelist = new DiscordBot.Models.DiscordBotWhitelist();
            }

            Config.DiscordBotWhitelist.Clear();
            Config.DiscordBotWhitelist.AddRange(DiscordBotWhitelist.Select(i => i.BotId));

            App.ReconfigureLogging();
        }

        private string GetDeployedVersion()
        {
            XmlDocument xmlDoc = new XmlDocument();
            Assembly asmCurrent = System.Reflection.Assembly.GetEntryAssembly();
            string executePath = new Uri(asmCurrent.GetName().CodeBase).LocalPath;

            xmlDoc.Load(executePath + ".manifest");
            XmlNamespaceManager ns = new XmlNamespaceManager(xmlDoc.NameTable);
            ns.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
            string xPath = "/asmv1:assembly/asmv1:assemblyIdentity/@version";
            XmlNode node = xmlDoc.SelectSingleNode(xPath, ns);
            string version = node.Value;
            return version;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Window.GetWindow(this)?.Activate();
        }

        private void ApplySteamAPIKey_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(CommonConfig.Default.SteamAPIKeyUrl);
        }

        private async void SendTestEmail_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                await Task.Run(() =>
                {
                    var email = new EmailUtil()
                    {
                        EnableSsl = Config.Default.Email_UseSSL,
                        MailServer = Config.Default.Email_Host,
                        Port = Config.Default.Email_Port,
                        UseDefaultCredentials = Config.Default.Email_UseDetaultCredentials,
                        Credentials = Config.Default.Email_UseDetaultCredentials ? null : new System.Net.NetworkCredential(Config.Default.Email_Username, Config.Default.Email_Password),
                    };

                    email.SendEmail(Config.Default.Email_From, Config.Default.Email_To?.Split(','), "Server Manager Test Email", "This is a test email sent from the server manager settings window.", true);

                });
                MessageBox.Show("Test email sent.", "Send Email Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (ex.InnerException != null)
                    message += $"\r\n{ex.InnerException.Message}";
                MessageBox.Show(message, "Send Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        public void SetDataDir_Click(object sender, RoutedEventArgs args)
        {
            var optionResult = MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_DataDirectoryChange_ConfirmLabel"), _globalizer.GetResourceString("GlobalSettings_DataDirectoryChange_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (optionResult == MessageBoxResult.Yes)
            {
                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                dialog.Title = _globalizer.GetResourceString("Application_DataDirectoryTitle");
                dialog.InitialDirectory = Config.Default.DataPath;
                var result = dialog.ShowDialog(Window.GetWindow(this));

                if (result != CommonFileDialogResult.Ok)
                    return;

                if (string.Equals(dialog.FileName, Config.Default.DataPath, StringComparison.OrdinalIgnoreCase))
                    return;

                try
                {
                    var newDataDirectory = IOUtils.NormalizeFolder(dialog.FileName);

                    // Set up the destination directories
                    string newConfigDirectory = Path.Combine(newDataDirectory, Config.Default.ProfilesRelativePath);
                    string oldSteamDirectory = Path.Combine(Config.Default.DataPath, CommonConfig.Default.SteamCmdRelativePath);
                    string newSteamDirectory = Path.Combine(newDataDirectory, CommonConfig.Default.SteamCmdRelativePath);

                    Directory.CreateDirectory(newConfigDirectory);
                    Directory.CreateDirectory(newSteamDirectory);

                    // Copy the Profiles
                    foreach (var file in Directory.EnumerateFiles(Config.Default.ConfigPath, "*.*", SearchOption.AllDirectories))
                    {
                        string sourceWithoutRoot = file.Substring(Config.Default.ConfigPath.Length + 1);
                        string destination = Path.Combine(newConfigDirectory, sourceWithoutRoot);
                        if (!File.Exists(destination))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destination));
                            File.Copy(file, destination);
                        }
                    }

                    // Copy the SteamCMD files
                    foreach (var file in Directory.EnumerateFiles(oldSteamDirectory, "*.*", SearchOption.AllDirectories))
                    {
                        string sourceWithoutRoot = file.Substring(oldSteamDirectory.Length + 1);
                        string destination = Path.Combine(newSteamDirectory, sourceWithoutRoot);
                        if (!File.Exists(destination))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destination));
                            File.Copy(file, destination);
                        }
                    }

                    // Remove the old directories
                    Directory.Delete(Config.Default.ConfigPath, true);
                    Directory.Delete(oldSteamDirectory, true);

                    // Update the config
                    Config.Default.DataPath = newDataDirectory;
                    Config.Default.ConfigPath = newConfigDirectory;
                    App.ReconfigureLogging();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format(_globalizer.GetResourceString("GlobalSettings_DataDirectoryChange_FailedLabel"), ex.Message), _globalizer.GetResourceString("GlobalSettings_DataDirectoryChange_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void ResetDataDir_Click(object sender, RoutedEventArgs e)
        {
            // Confirm the reset with the user.
            if (MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_ResetDataDirectory_ConfirmLabel"), _globalizer.GetResourceString("GlobalSettings_ResetDataDirectory_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            // Update the config
            Config.Default.DataPath = string.Empty;
            Config.Default.ConfigPath = string.Empty;

            App.SaveConfigFiles(false);

            Environment.Exit(0);
        }

        private void SetBackupDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = _globalizer.GetResourceString("GlobalSettings_DataDirectoryTitle");
            dialog.InitialDirectory = Config.Default.BackupPath;
            var result = dialog.ShowDialog(Window.GetWindow(this));

            if (result != CommonFileDialogResult.Ok)
                return;

            if (string.Equals(dialog.FileName, Config.Default.BackupPath, StringComparison.OrdinalIgnoreCase))
                return;

            Config.Default.BackupPath = IOUtils.NormalizeFolder(dialog.FileName);
        }

        private void ClearBackupDir_Click(object sender, RoutedEventArgs e)
        {
            Config.Default.BackupPath = string.Empty;
        }

        private void SetCacheDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = _globalizer.GetResourceString("GlobalSettings_CacheDirectoryTitle");
            dialog.InitialDirectory = Config.Default.DataPath;
            var result = dialog.ShowDialog(Window.GetWindow(this));

            if (result != CommonFileDialogResult.Ok)
                return;

            if (string.Equals(dialog.FileName, Config.Default.AutoUpdate_CacheDir, StringComparison.OrdinalIgnoreCase))
                return;

            Config.Default.AutoUpdate_CacheDir = IOUtils.NormalizeFolder(dialog.FileName);
        }

        private void SteamAPIKeyHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.Default.SteamWebAPIKeyHelpUrl);
        }

        private void DiscordBotApply_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.Default.DiscordBotApplyUrl);
        }

        private void DiscordBotHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.Default.DiscordBotHelpUrl);
        }

        private async void SteamCMDAuthenticate_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                if (string.IsNullOrWhiteSpace(Config.Default.SteamCmd_Username))
                {
                    MessageBox.Show("A steam username has not be entered.", "SteamCMD Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var steamCmdFile = SteamCmdUpdater.GetSteamCmdFile(Config.Default.DataPath);
                if (string.IsNullOrWhiteSpace(steamCmdFile) || !File.Exists(steamCmdFile))
                {
                    MessageBox.Show("Could not locate the SteamCMD executable. Try reinstalling SteamCMD.", "SteamCMD Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                var steamCmdArgs = SteamUtils.BuildSteamCmdArguments(CommonConfig.Default.SteamCmdRemoveQuit, CommonConfig.Default.SteamCmdAuthenticateArgs, Config.Default.SteamCmd_Username, Config.Default.SteamCmd_Password);
                var workingDirectory = Config.Default.DataPath;

                var SteamCmdIgnoreExitStatusCodes = SteamUtils.GetExitStatusList(Config.Default.SteamCmdIgnoreExitStatusCodes);

                var result = await ProcessUtils.RunProcessAsync(steamCmdFile, steamCmdArgs, string.Empty, workingDirectory, null, null, SteamCmdIgnoreExitStatusCodes, null, CancellationToken.None);
                if (result)
                    MessageBox.Show("The authentication was completed.", "SteamCMD Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("An error occurred while trying to authenticate with steam. Please try again.", "SteamCMD Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SteamCMD Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
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

        private void GameData_GameDataLoaded(object sender, EventArgs e)
        {
            PopulateRconMessageModesComboBox();
        }

        private void LanguageSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.CultureName = AvailableLanguages.Instance.SelectedLanguage;

            PopulateWindowsStatesMainWindowComboBox();
            PopulateWindowsStatesServerMonitorWindowComboBox();
            PopulateTaskPrioritiesComboBox();
            GameData_GameDataLoaded(sender, e);

            App.Instance.OnResourceDictionaryChanged(Config.CultureName);
        }

        private void StyleSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.StyleName = AvailableStyles.Instance.SelectedStyle;
        }

        private void HiddenField_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox hideTextBox)
            {
                TextBox textBox = null;
                if (Equals(hideTextBox, HideSteamPasswordTextBox))
                    textBox = SteamPasswordTextBox;
                if (Equals(hideTextBox, HideSteamAPIKeyTextBox))
                    textBox = SteamAPIKeyTextBox;
                if (Equals(hideTextBox, HideEmailPasswordTextBox))
                    textBox = EmailPasswordTextBox;
                if (Equals(hideTextBox, HideDiscordBotTokenTextBox))
                    textBox = DiscordBotTokenTextBox;
                if (Equals(hideTextBox, HideTaskSchedulerPasswordTextBox))
                    textBox = TaskSchedulerPasswordTextBox;

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
                if (textBox == SteamPasswordTextBox)
                    hideTextBox = HideSteamPasswordTextBox;
                if (textBox == SteamAPIKeyTextBox)
                    hideTextBox = HideSteamAPIKeyTextBox;
                if (textBox == EmailPasswordTextBox)
                    hideTextBox = HideEmailPasswordTextBox;
                if (textBox == DiscordBotTokenTextBox)
                    hideTextBox = HideDiscordBotTokenTextBox;
                if (textBox == TaskSchedulerPasswordTextBox)
                    hideTextBox = HideTaskSchedulerPasswordTextBox;

                if (hideTextBox != null)
                {
                    hideTextBox.Visibility = System.Windows.Visibility.Visible;
                    textBox.Visibility = System.Windows.Visibility.Collapsed;
                }
                UpdateLayout();
            }
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_ResetSettings_ConfirmLabel"), _globalizer.GetResourceString("GlobalSettings_ResetSettings_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            int exitCode = 0;

            try
            {
                Config.Default.Reset();
                Config.Default.UpgradeConfig = false;

                CommonConfig.Default.Reset();
                CommonConfig.Default.UpgradeConfig = false;

                App.SaveConfigFiles(false);
            }
            catch (Exception ex)
            {
                Logger.Error("Exception while resettiing the settings: {0}\n{1}", ex.Message, ex.StackTrace);
                MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_ResetSettings_FailedLabel"), _globalizer.GetResourceString("GlobalSettings_ResetSettings_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                exitCode = 1;
            }
            finally
            {
                Environment.Exit(exitCode);
            }
        }

        public void CloseControl()
        {
            GameData.GameDataLoaded -= GameData_GameDataLoaded;
        }

        private void PopulateWindowsStatesMainWindowComboBox()
        {
            var selectedValue = this.WindowStateMainWindowComboBox?.SelectedValue ?? Config.MainWindow_WindowState;
            var windowStates = new ComboBoxItemList();

            foreach (WindowState windowState in Enum.GetValues(typeof(WindowState)))
            {
                var displayMember = _globalizer.GetResourceString($"WindowState_{windowState}") ?? windowState.ToString();
                windowStates.Add(new Common.Model.ComboBoxItem(windowState.ToString(), displayMember));
            }

            this.WindowStatesMainWindow = windowStates;
            if (this.WindowStateMainWindowComboBox != null)
            {
                this.WindowStateMainWindowComboBox.SelectedValue = selectedValue;
            }
        }

        private void PopulateWindowsStatesServerMonitorWindowComboBox()
        {
            var selectedValue = this.WindowStateServerMonitorComboBox?.SelectedValue ?? Config.ServerMonitorWindow_WindowState;
            var comboBoxList = new ComboBoxItemList();

            foreach (WindowState windowState in Enum.GetValues(typeof(WindowState)))
            {
                var displayMember = _globalizer.GetResourceString($"WindowState_{windowState}") ?? windowState.ToString();
                comboBoxList.Add(new Common.Model.ComboBoxItem(windowState.ToString(), displayMember));
            }

            this.WindowStatesServerMonitor = comboBoxList;
            if (this.WindowStateServerMonitorComboBox != null)
            {
                this.WindowStateServerMonitorComboBox.SelectedValue = selectedValue;
            }
        }

        private void PopulateDiscordBotLogLevelsComboBox()
        {
            var selectedValue = this.DiscordBotLogLevelComboBox?.SelectedValue ?? Config.DiscordBotLogLevel;
            var comboBoxList = new ComboBoxItemList();

            foreach (DiscordBot.Enums.LogLevel logLevel in Enum.GetValues(typeof(DiscordBot.Enums.LogLevel)))
            {
                var displayMember = _globalizer.GetResourceString($"DiscordBotLogLevel_{logLevel}") ?? logLevel.ToString();
                comboBoxList.Add(new Common.Model.ComboBoxItem(logLevel.ToString(), displayMember));
            }

            this.DiscordBotLogLevels = comboBoxList;
            if (this.DiscordBotLogLevelComboBox != null)
            {
                this.DiscordBotLogLevelComboBox.SelectedValue = selectedValue;
            }
        }

        private void PopulateRconMessageModesComboBox()
        {
            var selectedValueBackup = this.RconBackupMessageModesComboBox?.SelectedValue ?? Config.RCON_BackupMessageCommand;
            var selectedValueAll = this.RconMessageModesComboBox?.SelectedValue ?? Config.RCON_MessageCommand;
            var list = new ComboBoxItemList();

            foreach (var item in GameData.GetMessageRconInputModes())
            {
                item.DisplayMember = GameData.FriendlyRconInputModeName(item.ValueMember);
                list.Add(item);
            }

            this.RconMessageModes = list;
            if (this.RconBackupMessageModesComboBox != null)
            {
                this.RconBackupMessageModesComboBox.SelectedValue = selectedValueBackup;
            }
            if (this.RconMessageModesComboBox != null)
            {
                this.RconMessageModesComboBox.SelectedValue = selectedValueAll;
            }
        }

        private void PopulateTaskPrioritiesComboBox()
        {
            var selectedValueAutoBackup = this.TaskPriorityAutoBackupComboBox?.SelectedValue ?? Config.AutoBackup_TaskPriority;
            var selectedValueAutoUpdate = this.TaskPriorityAutoUpdateComboBox?.SelectedValue ?? Config.AutoUpdate_TaskPriority;
            var selectedValueAutoShutdown = this.TaskPriorityAutoShutdownComboBox?.SelectedValue ?? Config.AutoShutdown_TaskPriority;
            var selectedValueAutoStart = this.TaskPriorityAutoStartComboBox?.SelectedValue ?? Config.AutoStart_TaskPriority;
            var list = new ComboBoxItemList();

            foreach (ProcessPriorityClass priority in Enum.GetValues(typeof(ProcessPriorityClass)))
            {
                var displayMember = _globalizer.GetResourceString($"TaskPriority_{priority}") ?? priority.ToString();
                list.Add(new Common.Model.ComboBoxItem(priority.ToString(), displayMember));
            }

            this.TaskPriorities = list;
            if (this.TaskPriorityAutoBackupComboBox != null)
            {
                this.TaskPriorityAutoBackupComboBox.SelectedValue = selectedValueAutoBackup;
            }
            if (this.TaskPriorityAutoUpdateComboBox != null)
            {
                this.TaskPriorityAutoUpdateComboBox.SelectedValue = selectedValueAutoUpdate;
            }
            if (this.TaskPriorityAutoShutdownComboBox != null)
            {
                this.TaskPriorityAutoShutdownComboBox.SelectedValue = selectedValueAutoShutdown;
            }
            if (this.TaskPriorityAutoStartComboBox != null)
            {
                this.TaskPriorityAutoStartComboBox.SelectedValue = selectedValueAutoStart;
            }
        }

        #region Discord Bot Whitelist
        private void AddDiscordBotWhitelist_Click(object sender, RoutedEventArgs e)
        {
            DiscordBotWhitelist.Add(new DiscordBotWhitelistItem());

            CollectionViewSource.GetDefaultView(DiscordBotWhitelistGrid.ItemsSource).Refresh();
        }

        private void ClearDiscordBotWhitelists_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            DiscordBotWhitelist.Clear();

            CollectionViewSource.GetDefaultView(DiscordBotWhitelistGrid.ItemsSource).Refresh();
        }

        private void RemoveDiscordBotWhitelist_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((DiscordBotWhitelistItem)((Button)e.Source).DataContext);
            DiscordBotWhitelist.Remove(item);

            CollectionViewSource.GetDefaultView(DiscordBotWhitelistGrid.ItemsSource).Refresh();
        }
        #endregion
    }
}
