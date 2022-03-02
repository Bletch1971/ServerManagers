using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for ProfileSyncWindow.xaml
    /// </summary>
    public partial class ProfileSyncWindow : Window
    {
        public class SyncProfile : DependencyObject
        {
            public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(bool), typeof(SyncProfile), new PropertyMetadata(false));
            public static readonly DependencyProperty ProfileNameProperty = DependencyProperty.Register(nameof(ProfileName), typeof(string), typeof(SyncProfile), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register(nameof(Profile), typeof(ServerProfile), typeof(SyncProfile), new PropertyMetadata(null));

            public bool Selected
            {
                get { return (bool)GetValue(SelectedProperty); }
                set { SetValue(SelectedProperty, value); }
            }
            public string ProfileName
            {
                get { return (string)GetValue(ProfileNameProperty); }
                set { SetValue(ProfileNameProperty, value); }
            }
            public ServerProfile Profile
            {
                get { return (ServerProfile)GetValue(ProfileProperty); }
                set { SetValue(ProfileProperty, value); }
            }
        }

        public class SyncSection : DependencyObject
        {
            public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(bool), typeof(SyncSection), new PropertyMetadata(false));
            public static readonly DependencyProperty SectionNameProperty = DependencyProperty.Register(nameof(SectionName), typeof(string), typeof(SyncSection), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(nameof(Category), typeof(ServerProfileCategory), typeof(SyncSection));

            public bool Selected
            {
                get { return (bool)GetValue(SelectedProperty); }
                set { SetValue(SelectedProperty, value); }
            }
            public string SectionName
            {
                get { return (string)GetValue(SectionNameProperty); }
                set { SetValue(SectionNameProperty, value); }
            }
            public ServerProfileCategory Category
            {
                get { return (ServerProfileCategory)GetValue(CategoryProperty); }
                set { SetValue(CategoryProperty, value); }
            }
        }

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty SyncProfilesProperty = DependencyProperty.Register(nameof(SyncProfiles), typeof(ObservableCollection<SyncProfile>), typeof(ProfileSyncWindow), new PropertyMetadata(new ObservableCollection<SyncProfile>()));
        public static readonly DependencyProperty SyncSectionsProperty = DependencyProperty.Register(nameof(SyncSections), typeof(ObservableCollection<SyncSection>), typeof(ProfileSyncWindow), new PropertyMetadata(new ObservableCollection<SyncSection>()));

        public ProfileSyncWindow(ServerManager serverManager, ServerProfile serverProfile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            OverlayGrid.Visibility = Visibility.Collapsed;

            this.Title = string.Format(_globalizer.GetResourceString("ProfileSyncWindow_ProfileTitle"), serverProfile?.ProfileName);

            this.ServerManager = serverManager;
            this.ServerProfile = serverProfile;
            this.DataContext = this;
        }

        public ServerManager ServerManager
        {
            get;
            private set;
        }

        public ServerProfile ServerProfile
        {
            get;
            private set;
        }

        public ObservableCollection<SyncProfile> SyncProfiles
        {
            get { return (ObservableCollection<SyncProfile>)GetValue(SyncProfilesProperty); }
            set { SetValue(SyncProfilesProperty, value); }
        }

        public ObservableCollection<SyncSection> SyncSections
        {
            get { return (ObservableCollection<SyncSection>)GetValue(SyncSectionsProperty); }
            set { SetValue(SyncSectionsProperty, value); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateSyncProfileList();
                CreateSyncSectionList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ProfileSyncWindow_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Process_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                if (!SyncProfiles.Any(s => s.Selected))
                {
                    MessageBox.Show(_globalizer.GetResourceString("ProfileSyncWindow_Process_NoProfilesSelectedLabel"), _globalizer.GetResourceString("ProfileSyncWindow_Process_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!SyncSections.Any(s => s.Selected))
                {
                    MessageBox.Show(_globalizer.GetResourceString("ProfileSyncWindow_Process_NoSectionsSelectedLabel"), _globalizer.GetResourceString("ProfileSyncWindow_Process_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBox.Show(_globalizer.GetResourceString("ProfileSyncWindow_Process_ConfirmLabel"), _globalizer.GetResourceString("ProfileSyncWindow_Process_Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);

                OverlayProfile.Content = string.Empty;
                OverlaySection.Content = string.Empty;
                OverlayGrid.Visibility = Visibility.Visible;
                await Task.Delay(100);

                await PerformProfileSync();

                MessageBox.Show(_globalizer.GetResourceString("ProfileSyncWindow_Process_SuccessLabel"), _globalizer.GetResourceString("ProfileSyncWindow_Process_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ProfileSyncWindow_Process_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                OverlayGrid.Visibility = Visibility.Collapsed;

                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void ProfileSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var profile in SyncProfiles)
            {
                profile.Selected = true;
            }
        }

        private void ProfileUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var profile in SyncProfiles)
            {
                profile.Selected = false;
            }
        }

        private void SectionSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var section in SyncSections)
            {
                section.Selected = true;
            }
        }

        private void SectionUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var section in SyncSections)
            {
                section.Selected = false;
            }
        }

        private void CreateSyncProfileList()
        {
            SyncProfiles.Clear();

            if (this.ServerManager == null || this.ServerManager.Servers == null)
                return;

            foreach (var server in this.ServerManager.Servers)
            {
                if (server.Profile == ServerProfile)
                    continue;

                SyncProfiles.Add(new SyncProfile() { Selected = false, Profile = server.Profile, ProfileName = server.Profile.ProfileName });
            }
        }

        private void CreateSyncSectionList()
        {
            SyncSections.Clear();

            SyncSections.Add(new SyncSection() { Selected = false, Category = ServerProfileCategory.Administration, SectionName = _globalizer.GetResourceString("ServerSettings_AdministrationSectionLabel") });
            SyncSections.Add(new SyncSection() { Selected = false, Category = ServerProfileCategory.AutomaticManagement, SectionName = _globalizer.GetResourceString("ServerSettings_AutomaticManagementLabel") });
            if (Config.Default.DiscordBotEnabled)
                SyncSections.Add(new SyncSection() { Selected = false, Category = ServerProfileCategory.DiscordBot, SectionName = _globalizer.GetResourceString("ServerSettings_DiscordBotLabel") });
            SyncSections.Add(new SyncSection() { Selected = false, Category = ServerProfileCategory.ServerDetails, SectionName = _globalizer.GetResourceString("ServerSettings_ServerDetailsLabel") });
            SyncSections.Add(new SyncSection() { Selected = false, Category = ServerProfileCategory.ServerFiles, SectionName = _globalizer.GetResourceString("ServerSettings_ServerFilesLabel") });
        }

        private async Task PerformProfileSync()
        {
            foreach (var syncProfile in SyncProfiles)
            {
                if (!syncProfile.Selected)
                    continue;

                OverlayProfile.Content = syncProfile.ProfileName ?? string.Empty;

                bool updateSchedules = false;

                foreach (var syncSection in SyncSections)
                {
                    if (!syncSection.Selected)
                        continue;

                    OverlaySection.Content = syncSection.SectionName;
                    await Task.Delay(100);

                    syncProfile.Profile?.SyncSettings(syncSection.Category, ServerProfile);

                    if (syncSection.Category == ServerProfileCategory.AutomaticManagement)
                        updateSchedules = true;
                }

                syncProfile.Profile?.Save(false, updateSchedules, null);
            }
        }
    }
}
