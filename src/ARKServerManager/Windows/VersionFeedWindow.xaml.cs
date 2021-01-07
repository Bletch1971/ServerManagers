using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for VersionFeedWindow.xaml
    /// </summary>
    public partial class VersionFeedWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty AppInstanceProperty = DependencyProperty.Register(nameof(AppInstance), typeof(App), typeof(VersionFeedWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty FeedEntriesProperty = DependencyProperty.Register(nameof(FeedEntries), typeof(ObservableCollection<VersionFeedEntry>), typeof(VersionFeedWindow), new PropertyMetadata(new ObservableCollection<VersionFeedEntry>()));
        public static readonly DependencyProperty SelectedFeedEntryProperty = DependencyProperty.Register(nameof(SelectedFeedEntry), typeof(VersionFeedEntry), typeof(VersionFeedWindow), new PropertyMetadata(null));

        private string feedUri = string.Empty;

        public VersionFeedWindow(string feedUri)
        {
            this.AppInstance = App.Instance;

            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.feedUri = feedUri;

            this.DataContext = this;
        }

        public App AppInstance
        {
            get { return GetValue(AppInstanceProperty) as App; }
            set { SetValue(AppInstanceProperty, value); }
        }

        public ObservableCollection<VersionFeedEntry> FeedEntries
        {
            get { return (ObservableCollection<VersionFeedEntry>)GetValue(FeedEntriesProperty); }
            set { SetValue(FeedEntriesProperty, value); }
        }

        public VersionFeedEntry SelectedFeedEntry
        {
            get { return (VersionFeedEntry)GetValue(SelectedFeedEntryProperty); }
            set { SetValue(SelectedFeedEntryProperty, value); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadFeed();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("VersionFeedWindow_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PatchNotes_Click(object sender, RoutedEventArgs e)
        {
            var url = string.Empty;
            if (AppInstance.BetaVersion)
                url = Config.Default.LatestASMBetaPatchNotesUrl;
            else
                url = Config.Default.LatestASMPatchNotesUrl;

            if (string.IsNullOrWhiteSpace(url))
                return;

            Process.Start(url);
        }

        private void LoadFeed()
        {
            FeedEntries.Clear();

            if (string.IsNullOrWhiteSpace(this.feedUri))
                return;

            var versionFeed = VersionFeedUtils.LoadVersionFeed(this.feedUri, App.Instance.Version);
            if (versionFeed == null)
                return;

            foreach (var entry in versionFeed.Entries)
            {
                if (entry == null)
                    continue;

                FeedEntries.Add(entry);
            }

            SelectedFeedEntry = FeedEntries.OrderByDescending(e => e.Updated).FirstOrDefault();
        }
    }
}
