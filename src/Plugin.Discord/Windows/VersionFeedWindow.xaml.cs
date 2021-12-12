using ServerManagerTool.Plugin.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ServerManagerTool.Plugin.Discord.Windows
{
    /// <summary>
    /// Interaction logic for VersionFeedWindow.xaml
    /// </summary>
    public partial class VersionFeedWindow : Window
    {
        public static readonly DependencyProperty FeedEntriesProperty = DependencyProperty.Register(nameof(FeedEntries), typeof(ObservableCollection<VersionFeedEntry>), typeof(VersionFeedWindow), new PropertyMetadata(new ObservableCollection<VersionFeedEntry>()));
        public static readonly DependencyProperty SelectedFeedEntryProperty = DependencyProperty.Register(nameof(SelectedFeedEntry), typeof(VersionFeedEntry), typeof(VersionFeedWindow), new PropertyMetadata(null));

        private string feedUri = string.Empty;

        public VersionFeedWindow(DiscordPlugin plugin, string feedUri)
        {
            InitializeComponent();
            WindowUtils.UpdateResourceDictionary(this, plugin.LanguageCode);

            this.Plugin = plugin ?? new DiscordPlugin();
            this.feedUri = feedUri;

            this.DataContext = this;
        }

        private DiscordPlugin Plugin
        {
            get;
            set;
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

        private void VersionFeedWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadFeed();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ResourceUtils.GetResourceString(this.Resources, "VersionFeedWindow_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFeed()
        {
            FeedEntries.Clear();

            if (string.IsNullOrWhiteSpace(this.feedUri))
                return;

            var versionFeed = VersionFeedUtils.LoadVersionFeed(this.feedUri, Plugin.PluginVersion.ToString());
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
