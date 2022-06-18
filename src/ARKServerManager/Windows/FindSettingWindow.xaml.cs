using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Extensions;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for FindSettingWindow.xaml
    /// </summary>
    public partial class FindSettingWindow : Window
    {
        private static List<(string setting, string profileProperty)> _profileSettings = null;
        private static List<(string setting, Control control)> _settingControls = null;

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private int _controlIndex = -1;
        private ServerSettingsControl _serverSettingsControl;

        public static readonly DependencyProperty FindSettingStringProperty = DependencyProperty.Register(nameof(FindSettingString), typeof(string), typeof(FindSettingWindow), new PropertyMetadata(""));

        public FindSettingWindow(ServerSettingsControl control)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            _serverSettingsControl = control;

            LoadSettings(control.Settings);
            LoadControls(control);

            this.DataContext = this;
        }

        public string FindSettingString
        {
            get { return (string)GetValue(FindSettingStringProperty); }
            set { SetValue(FindSettingStringProperty, value); }
        }

        private async void Find_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FindSettingString))
                return;

            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(100);

                _serverSettingsControl.UnselectControl();

                var findSettingString = FindSettingString.Trim();
                var foundControls = _settingControls
                    .Where(s => s.setting.Contains(findSettingString, StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.control)
                    .ToArray();
                if (foundControls.Length == 0)
                {
                    MessageBox.Show(string.Format(_globalizer.GetResourceString("FindSettingWindow_NotFoundErrorLabel"), FindSettingString), _globalizer.GetResourceString("FindSettingWindow_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var oldIndex = _controlIndex;
                var newIndex = oldIndex;

                while (true)
                {
                    newIndex += 1;
                    if (newIndex >= foundControls.Length)
                    {
                        _controlIndex = -1;
                        MessageBox.Show(string.Format(_globalizer.GetResourceString("FindSettingWindow_NotFoundErrorLabel"), FindSettingString), _globalizer.GetResourceString("FindSettingWindow_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var selected = _serverSettingsControl.SelectControl(foundControls[newIndex]);
                    if (selected)
                    {
                        _controlIndex = newIndex;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("FindSettingWindow_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void LoadControls(DependencyObject parent)
        {
            if (_settingControls != null)
                return;

            try
            {
                _settingControls = WindowUtils.GetLogicalTreeControls(parent);
                for (int i = 0; i < _settingControls.Count; i++)
                {
                    var item = _settingControls[i];
                    var setting = _profileSettings
                        .FirstOrDefault(x => x.profileProperty.Equals(item.setting, StringComparison.OrdinalIgnoreCase))
                        .setting;

                    if (setting != null && !setting.Equals(item.setting, StringComparison.OrdinalIgnoreCase))
                    {
                        _settingControls[i] = (setting, item.control);
                    }

#if DEBUG
                    Debug.WriteLine($"{_settingControls[i].setting}; {_settingControls[i].control.GetType().FullName}");
#endif
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("FindSettingWindow_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSettings(ServerProfile profile)
        {
            if (_profileSettings != null)
                return;

            try
            {
                _profileSettings = new List<(string setting, string profileProperty)>();

                var fields = profile?.GetType()
                    .GetProperties()
                    .Where(f => f.IsDefined(typeof(BaseIniFileEntryAttribute), false));

                foreach (var field in fields)
                {
                    var attributes = field
                        .GetCustomAttributes(typeof(BaseIniFileEntryAttribute), false)
                        .OfType<BaseIniFileEntryAttribute>();

                    foreach (var attr in attributes)
                    {
                        _profileSettings.Add((string.IsNullOrWhiteSpace(attr.Key) ? field.Name : attr.Key, field.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("FindSettingWindow_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FindSettingString_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            _serverSettingsControl.UnselectControl();
            _controlIndex = -1;
        }
    }
}
