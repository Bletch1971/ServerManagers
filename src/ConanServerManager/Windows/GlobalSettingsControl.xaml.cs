﻿using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using ServerManagerTool.Common;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty AppInstanceProperty = DependencyProperty.Register(nameof(AppInstance), typeof(App), typeof(GlobalSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty IsAdministratorProperty = DependencyProperty.Register(nameof(IsAdministrator), typeof(bool), typeof(GlobalSettingsControl), new PropertyMetadata(false));

        public GlobalSettingsControl()
        {
            this.AppInstance = App.Instance;
            this.Config = Config.Default;
            this.CommonConfig = CommonConfig.Default;
            this.IsAdministrator = SecurityUtils.IsAdministrator();
            this.Version = GetDeployedVersion();

            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.DataContext = this;
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

        public string Version
        {
            get;
            set;
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

                if (result == CommonFileDialogResult.Ok)
                {
                    if (!String.Equals(dialog.FileName, Config.Default.DataPath))
                    {
                        try
                        {
                            // Set up the destination directories
                            string newConfigDirectory = Path.Combine(dialog.FileName, Config.Default.ProfilesRelativePath);
                            string oldSteamDirectory = Path.Combine(Config.Default.DataPath, CommonConfig.Default.SteamCmdRelativePath);
                            string newSteamDirectory = Path.Combine(dialog.FileName, CommonConfig.Default.SteamCmdRelativePath);

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
                            Config.Default.DataPath = dialog.FileName;
                            Config.Default.ConfigPath = newConfigDirectory;
                            App.ReconfigureLogging();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format(_globalizer.GetResourceString("GlobalSettings_DataDirectoryChange_FailedLabel"), ex.Message), _globalizer.GetResourceString("GlobalSettings_DataDirectoryChange_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }

                    }
                }
            }
        }

        private void SetBackupDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = _globalizer.GetResourceString("GlobalSettings_DataDirectoryTitle");
            dialog.InitialDirectory = Config.Default.BackupPath;
            var result = dialog.ShowDialog(Window.GetWindow(this));

            if (result == CommonFileDialogResult.Ok)
            {
                if (!String.Equals(dialog.FileName, Config.Default.BackupPath))
                {
                    Config.Default.BackupPath = dialog.FileName;
                }
            }
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

            if (result == CommonFileDialogResult.Ok)
            {
                if (!String.Equals(dialog.FileName, Config.Default.AutoUpdate_CacheDir))
                {
                    Config.Default.AutoUpdate_CacheDir = dialog.FileName;
                }
            }
        }

        private void SteamAPIKeyHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.Default.SteamWebAPIKeyHelpUrl);
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

                var result = await ProcessUtils.RunProcessAsync(steamCmdFile, steamCmdArgs, string.Empty, workingDirectory, null, null, null, CancellationToken.None);
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

        private void LanguageSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.CultureName = AvailableLanguages.Instance.SelectedLanguage;
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
                Config.Default.Save();
                Config.Default.Reload();

                CommonConfig.Default.Reset();
                CommonConfig.Default.UpgradeConfig = false;
                CommonConfig.Default.Save();
                CommonConfig.Default.Reload();
            }
            catch (Exception ex)
            {
                Logger.Error("Exception while resettiing the settings: {0}\n{1}", ex.Message, ex.StackTrace);
                MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_ResetSettings_FailedLabel"), _globalizer.GetResourceString("GlobalSettings_ResetSettings_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                exitCode = 1;
            }
            finally
            {
                App.Current.Shutdown(exitCode);
            }
        }
    }
}
