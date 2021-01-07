using Microsoft.WindowsAPICodePack.Dialogs;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Plugin.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for PluginsWindow.xaml
    /// </summary>
    public partial class PluginsWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty PluginHelperInstanceProperty = DependencyProperty.Register(nameof(PluginHelperInstance), typeof(PluginHelper), typeof(PluginsWindow));

        public PluginsWindow()
        {
            this.PluginHelperInstance = PluginHelper.Instance;

            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.DataContext = this;
        }

        public PluginHelper PluginHelperInstance
        {
            get { return GetValue(PluginHelperInstanceProperty) as PluginHelper; }
            set { SetValue(PluginHelperInstanceProperty, value); }
        }

        private void AddPlugin_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Title = GlobalizedApplication.Instance.GetResourceString("PluginsWindow_AddDialogTitle");
            dialog.DefaultExtension = GlobalizedApplication.Instance.GetResourceString("PluginsWindow_PluginDefaultExtension");
            dialog.Filters.Add(new CommonFileDialogFilter(GlobalizedApplication.Instance.GetResourceString("PluginsWindow_AddFilterLabel"), GlobalizedApplication.Instance.GetResourceString("PluginsWindow_AddFilterExtension")));
            if (dialog == null || dialog.ShowDialog(this) != CommonFileDialogResult.Ok)
                return;

            try
            {
                var installPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                PluginHelperInstance.AddPlugin(installPath, dialog.FileName);
            }
            catch (PluginException ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("PluginsWindow_AddErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                MessageBox.Show(_globalizer.GetResourceString("PluginsWindow_AddErrorLabel"), _globalizer.GetResourceString("PluginsWindow_AddErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearPlugins_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("PluginsWindow_ClearLabel"), _globalizer.GetResourceString("PluginsWindow_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                PluginHelperInstance.DeleteAllPlugins();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("PluginsWindow_ClearErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pluginItem = ((PluginItem)((Button)e.Source).DataContext);
                PluginHelperInstance.OpenConfigForm(pluginItem.Plugin, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("PluginsWindow_ConfigErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenPluginsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", PluginHelper.PluginFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("PluginsWindow_OpenErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PluginsForum_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.Default.ServerManagerPluginUrl);
        }

        private void RemovePlugin_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("PluginsWindow_DeleteLabel"), _globalizer.GetResourceString("PluginsWindow_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var pluginItem = ((PluginItem)((Button)e.Source).DataContext);
                PluginHelperInstance.DeletePlugin(pluginItem.PluginFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("PluginsWindow_DeleteErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
