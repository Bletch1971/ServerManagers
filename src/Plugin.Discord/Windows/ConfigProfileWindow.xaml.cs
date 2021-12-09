using ServerManagerTool.Plugin.Common;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ServerManagerTool.Plugin.Discord.Windows
{
    /// <summary>
    /// Interaction logic for ConfigProfileWindow.xaml
    /// </summary>
    public partial class ConfigProfileWindow : Window
    {
        private static readonly DependencyProperty AlertsListProperty = DependencyProperty.Register(nameof(AlertTypeList), typeof(ComboBoxItemList), typeof(ConfigProfileWindow));
        private static readonly DependencyProperty ProfileProperty = DependencyProperty.Register(nameof(Profile), typeof(ConfigProfile), typeof(ConfigProfileWindow));
        private static readonly DependencyProperty ProfileListProperty = DependencyProperty.Register(nameof(ProfileList), typeof(ComboBoxItemList), typeof(ConfigProfileWindow));

        internal ConfigProfileWindow(DiscordPlugin plugin, ConfigProfile profile)
        {
            InitializeComponent();
            WindowUtils.UpdateResourceDictionary(this, plugin.LanguageCode);

            this.Plugin = plugin;
            this.OriginalProfile = profile;
            this.Profile = profile.Clone();
            this.Profile.CommitChanges();

            RefreshAlertTypeList();
            RefreshProfileList();

            if (plugin.BetaEnabled)
                Title = $"{Title} {ResourceUtils.GetResourceString(this.Resources, "Global_BetaModeLabel")}";

            this.DataContext = this;
        }

        private ComboBoxItemList AlertTypeList
        {
            get { return GetValue(AlertsListProperty) as ComboBoxItemList; }
            set { SetValue(AlertsListProperty, value); }
        }

        private ConfigProfile OriginalProfile
        {
            get;
            set;
        }

        private ConfigProfile Profile
        {
            get { return GetValue(ProfileProperty) as ConfigProfile; }
            set { SetValue(ProfileProperty, value); }
        }

        private DiscordPlugin Plugin
        {
            get;
            set;
        }

        private ComboBoxItemList ProfileList
        {
            get { return GetValue(ProfileListProperty) as ComboBoxItemList; }
            set { SetValue(ProfileListProperty, value); }
        }

        private void ConfigProfileWindow_Closing(object sender, CancelEventArgs e)
        {
            if (DialogResult.HasValue && DialogResult.Value)
                return;

            if (this.Profile.HasAnyChanges)
            {
                if (MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_CloseLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_CloseTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
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

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (this.Profile.HasAnyChanges)
            {
                this.OriginalProfile.CopyFrom(this.Profile);
            }

            DialogResult = true;
            Close();
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            if (!Profile.IsEnabled)
            {
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_TestEnabledErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_TestErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                foreach (var profileName in Profile.ProfileNames)
                {
                    foreach (var alertType in Profile.AlertTypes)
                    {
                        var testMessage = string.Format(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_TestAlertMessage"), alertType.Value, profileName.Value);
                        Plugin.HandleAlert(Profile, alertType.Value, profileName.Value, testMessage);
                        Task.Delay(1000).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(Test_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_TestErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_TestErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddAlertType_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var alertType = new AlertTypeValue(AlertType.Error);

                Profile.AlertTypes.Add(alertType);
                Profile.AlertTypes.NotifyAdd(alertType);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(AddAlertType_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_AddAlertTypeErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_AddErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearAlertTypes_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_ClearLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                if (Profile.AlertTypes.Count == 0)
                    return;

                Profile.AlertTypes.Clear();
                Profile.AlertTypes.NotifyClear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(ClearAlertTypes_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_ClearAlertTypesErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_ClearErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAlertType_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_DeleteLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var alertType = ((AlertTypeValue)((Button)e.Source).DataContext);
                var index = Profile.AlertTypes.IndexOf(alertType);
                Profile.AlertTypes.Remove(alertType);
                Profile.AlertTypes.NotifyRemove(alertType, index);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(DeleteAlertType_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_DeleteAlertTypeErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_DeleteErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddProfileName_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profileName = new ProfileNameValue();

                Profile.ProfileNames.Add(profileName);
                Profile.ProfileNames.NotifyAdd(profileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(AddProfileName_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_AddProfileNameErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_AddErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearProfileNames_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_ClearLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                if (Profile.ProfileNames.Count == 0)
                    return;

                Profile.ProfileNames.Clear();
                Profile.ProfileNames.NotifyClear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(ClearProfileNames_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_ClearProfileNamesErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_ClearErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteProfileName_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_DeleteLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var profileName = ((ProfileNameValue)((Button)e.Source).DataContext);
                var index = Profile.ProfileNames.IndexOf(profileName);
                Profile.ProfileNames.Remove(profileName);
                Profile.ProfileNames.NotifyRemove(profileName, index);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(DeleteProfileName_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_DeleteProfileNameErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigProfileWindow_DeleteErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            try
            {
                comboBox.BeginInit();
            }
            finally
            {
                comboBox.EndInit();
            }
        }

        private void ComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            if (comboBox.SelectedItem == null)
            {
                var text = comboBox.Text;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    var source = comboBox.ItemsSource as ComboBoxItemList;
                    source?.Add(new ComboBoxItem
                    {
                        ValueMember = text,
                        DisplayMember = text,
                    });
                }

                comboBox.SelectedValue = text;
            }

            var expression = comboBox.GetBindingExpression(Selector.SelectedValueProperty);
            expression?.UpdateSource();

            expression = comboBox.GetBindingExpression(ComboBox.TextProperty);
            expression?.UpdateSource();
        }

        private void RefreshAlertTypeList()
        {
            var newList = new ComboBoxItemList();

            foreach (AlertType alertType in Enum.GetValues(typeof(AlertType)))
            {
                newList.Add(new ComboBoxItem
                {
                    ValueMember = alertType.ToString(),
                    DisplayMember = ResourceUtils.GetResourceString(this.Resources, $"AlertType_{alertType}") ?? alertType.ToString(),
                });
            }

            newList.Sort(i => i.DisplayMember);
            this.AlertTypeList = newList;
        }

        private void RefreshProfileList()
        {
            var newList = new ComboBoxItemList();
            newList.Add(new ComboBoxItem
            {
                ValueMember = string.Empty,
                DisplayMember = string.Empty,
            });

            try
            {
                foreach (var profile in PluginHelper.Instance.FetchProfileList())
                {
                    newList.Add(new ComboBoxItem
                    {
                        ValueMember = profile.ProfileName,
                        DisplayMember = $"* {profile.ProfileName}",
                    });
                }
            }
            catch (Exception)
            {
                // do nothing, most likely they are using an older version of a server manager
            }

            foreach (var profile in Profile.ProfileNames)
            {
                if (!newList.Any(p => p.ValueMember.Equals(profile.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    newList.Add(new ComboBoxItem
                    {
                        ValueMember = profile.Value,
                        DisplayMember = profile.Value,
                    });
                }
            }

            this.ProfileList = newList;
        }
    }
}
