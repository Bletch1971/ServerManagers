using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using ServerManagerTool.Lib.ViewModel.RCON;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for PlayerListWindow.xaml
    /// </summary>
    public partial class PlayerListWindow : Window
    {
        private static Dictionary<Server, PlayerListWindow> Windows = new Dictionary<Server, PlayerListWindow>();

        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty ConfigProperty = DependencyProperty.Register(nameof(Config), typeof(Config), typeof(PlayerListWindow), new PropertyMetadata(Config.Default));
        public static readonly DependencyProperty PlayerFilteringProperty = DependencyProperty.Register(nameof(PlayerFiltering), typeof(PlayerFilterType), typeof(PlayerListWindow), new PropertyMetadata(PlayerFilterType.Online | PlayerFilterType.Offline));
        public static readonly DependencyProperty PlayerListParametersProperty = DependencyProperty.Register(nameof(PlayerListParameters), typeof(PlayerListParameters), typeof(PlayerListWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty PlayerListFilterStringProperty = DependencyProperty.Register(nameof(PlayerListFilterString), typeof(string), typeof(PlayerListWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty PlayerListViewProperty = DependencyProperty.Register(nameof(PlayerListView), typeof(ICollectionView), typeof(PlayerListWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty PlayerSortingProperty = DependencyProperty.Register(nameof(PlayerSorting), typeof(PlayerSortType), typeof(PlayerListWindow), new PropertyMetadata(PlayerSortType.Name));
        public static readonly DependencyProperty ServerPlayersProperty = DependencyProperty.Register(nameof(ServerPlayers), typeof(ServerPlayers), typeof(PlayerListWindow), new PropertyMetadata(null));

        public PlayerListWindow(PlayerListParameters parameters)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.PlayerFiltering = (PlayerFilterType)Config.Default.PlayerListFilter;
            this.PlayerSorting = (PlayerSortType)Config.Default.PlayerListSort;

            this.PlayerListParameters = parameters;
            this.ServerPlayers = new ServerPlayers(parameters);
            this.ServerPlayers.Players.CollectionChanged += Players_CollectionChanged;
            this.ServerPlayers.PlayersCollectionUpdated += Players_CollectionUpdated;

            this.PlayerListView = CollectionViewSource.GetDefaultView(this.ServerPlayers.Players);
            this.PlayerListView.Filter = new Predicate<object>(PlayerListFilter);

            this.DataContext = this;

            if (this.PlayerListParameters.WindowExtents.Width > 50 && this.PlayerListParameters.WindowExtents.Height > 50)
            {
                this.Left = this.PlayerListParameters.WindowExtents.Left;
                this.Top = this.PlayerListParameters.WindowExtents.Top;
                this.Width = this.PlayerListParameters.WindowExtents.Width;
                this.Height = this.PlayerListParameters.WindowExtents.Height;

                // Fix issues where the window was saved while offscreen.
                if (this.Left == -32000)
                {
                    this.Left = 0;
                }

                if (this.Top == -32000)
                {
                    this.Top = 0;
                }
            }
        }

        public Config Config
        {
            get { return (Config)GetValue(ConfigProperty); }
            set { SetValue(ConfigProperty, value); }
        }

        public PlayerListParameters PlayerListParameters
        {
            get { return (PlayerListParameters)GetValue(PlayerListParametersProperty); }
            set { SetValue(PlayerListParametersProperty, value); }
        }

        public PlayerFilterType PlayerFiltering
        {
            get { return (PlayerFilterType)GetValue(PlayerFilteringProperty); }
            set { SetValue(PlayerFilteringProperty, value); }
        }

        public string PlayerListFilterString
        {
            get { return (string)GetValue(PlayerListFilterStringProperty); }
            set { SetValue(PlayerListFilterStringProperty, value); }
        }

        public ICollectionView PlayerListView
        {
            get { return (ICollectionView)GetValue(PlayerListViewProperty); }
            set { SetValue(PlayerListViewProperty, value); }
        }

        public PlayerSortType PlayerSorting
        {
            get { return (PlayerSortType)GetValue(PlayerSortingProperty); }
            set { SetValue(PlayerSortingProperty, value); }
        }

        public ServerPlayers ServerPlayers
        {
            get { return (ServerPlayers)GetValue(ServerPlayersProperty); }
            set { SetValue(ServerPlayersProperty, value); }
        }

        public ICommand CopyIDCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) =>
                    {
                        try
                        {
                            System.Windows.Clipboard.SetText(player.PlayerId.ToString());
                            MessageBox.Show($"{_globalizer.GetResourceString("RCON_CopyIdLabel")} {player.PlayerName}", _globalizer.GetResourceString("RCON_CopyIdTitle"), MessageBoxButton.OK);
                        }
                        catch
                        {
                            MessageBox.Show($"{_globalizer.GetResourceString("RCON_ClipboardErrorLabel")}", _globalizer.GetResourceString("RCON_ClipboardErrorTitle"), MessageBoxButton.OK);
                        }
                    },
                    canExecute: (player) => player != null
                    );

            }
        }

        public ICommand CopyPlayerIDCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) =>
                    {
                        if (player.PlayerData != null)
                        {
                            try
                            {
                                System.Windows.Clipboard.SetText(player.PlayerData.CharacterId.ToString());
                                MessageBox.Show($"{_globalizer.GetResourceString("RCON_CopyPlayerIdLabel")} {player.PlayerName}", _globalizer.GetResourceString("RCON_CopyPlayerIdTitle"), MessageBoxButton.OK);
                            }
                            catch
                            {
                                MessageBox.Show($"{_globalizer.GetResourceString("RCON_ClipboardErrorLabel")}", _globalizer.GetResourceString("RCON_ClipboardErrorTitle"), MessageBoxButton.OK);
                            }
                        }
                    },
                    canExecute: (player) => player?.PlayerData != null && player.IsValid
                    );

            }
        }

        public ICommand FilterPlayersCommand
        {
            get
            {
                return new RelayCommand<PlayerFilterType>(
                    execute: (filter) =>
                    {
                        this.PlayerFiltering ^= filter;
                        Config.Default.PlayerListFilter = (int)this.PlayerFiltering;
                        this.PlayerListView.Refresh();
                    },
                    canExecute: (filter) => true
                );
            }
        }

        public ICommand SortPlayersCommand
        {
            get
            {
                return new RelayCommand<PlayerSortType>(
                    execute: (sort) =>
                    {
                        Config.Default.PlayerListSort = (int)this.PlayerSorting;
                        SortPlayers();
                    },
                    canExecute: (sort) => true
                );
            }
        }

        public ICommand ViewPlayerProfileCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => {
                        var savedArksPath = ServerProfile.GetProfileSavePath(this.PlayerListParameters.InstallDirectory, this.PlayerListParameters.AltSaveDirectoryName, this.PlayerListParameters.PGM_Enabled, this.PlayerListParameters.PGM_Name);
                        var window = new PlayerProfileWindow(player, savedArksPath);
                        window.Owner = this;
                        window.ShowDialog();
                    },
                    canExecute: (player) => player?.PlayerData != null
                    );
            }
        }

        public ICommand ViewPlayerTribeCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => {
                        var savedArksPath = ServerProfile.GetProfileSavePath(this.PlayerListParameters.InstallDirectory, this.PlayerListParameters.AltSaveDirectoryName, this.PlayerListParameters.PGM_Enabled, this.PlayerListParameters.PGM_Name);
                        var window = new TribeProfileWindow(player, this.ServerPlayers.Players, savedArksPath);
                        window.Owner = this;
                        window.ShowDialog();
                    },
                    canExecute: (player) => player?.PlayerData != null && !string.IsNullOrWhiteSpace(player.TribeName)
                    );
            }
        }

        private void Players_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            PlayerListView?.Refresh();
        }

        private void Players_CollectionUpdated(object sender, EventArgs e)
        {
            this.PlayerListView = CollectionViewSource.GetDefaultView(this.ServerPlayers.Players);
            this.PlayerListView.Filter = new Predicate<object>(PlayerListFilter);

            SortPlayers();
            PlayerListView?.Refresh();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState != WindowState.Minimized)
            {
                var savedRect = this.PlayerListParameters.WindowExtents;
                this.PlayerListParameters.WindowExtents = new Rect(savedRect.Location, e.NewSize);
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized && this.Left != -32000 && this.Top != -32000)
            {
                var savedRect = this.PlayerListParameters.WindowExtents;
                this.PlayerListParameters.WindowExtents = new Rect(new Point(this.Left, this.Top), savedRect.Size);
                if (this.PlayerListParameters.Server != null)
                {
                    this.PlayerListParameters.Server.Profile.PlayerListWindowExtents = this.PlayerListParameters.WindowExtents;
                }
            }
        }

        public static void CloseAllWindows()
        {
            var windows = Windows.Values.ToList();
            foreach (var window in windows)
            {
                if (window.IsLoaded)
                    window.Close();
            }
            windows.Clear();
        }

        public static PlayerListWindow GetWindowForServer(Server server)
        {
            if (!Windows.TryGetValue(server, out PlayerListWindow window) || !window.IsLoaded)
            {
                window = new PlayerListWindow(new PlayerListParameters()
                {
                    WindowTitle = String.Format(GlobalizedApplication.Instance.GetResourceString("PlayerList_TitleLabel"), server.Runtime.ProfileSnapshot.ProfileName),
                    WindowExtents = server.Profile.PlayerListWindowExtents,

                    Server = server,
                    ServerMap = ServerProfile.GetProfileMapName(server?.Profile),
                    InstallDirectory = server.Runtime.ProfileSnapshot.InstallDirectory,
                    ProfileName = server.Runtime.ProfileSnapshot.ProfileName,
                    ProfileId = server.Runtime.ProfileSnapshot.ProfileId,
                });
                Windows[server] = window;
            }

            return window;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.ServerPlayers.PlayersCollectionUpdated -= Players_CollectionUpdated;
            this.ServerPlayers.Players.CollectionChanged -= Players_CollectionChanged;
            this.ServerPlayers.Dispose();

            if (this.PlayerListParameters?.Server != null)
            {
                Windows.TryGetValue(this.PlayerListParameters.Server, out PlayerListWindow window);
                if (window != null)
                {
                    Windows.Remove(this.PlayerListParameters.Server);
                }
            }

            base.OnClosing(e);
        }

        public void SortPlayers()
        {
            this.PlayerListView.SortDescriptions.Clear();

            switch (this.PlayerSorting)
            {
                case PlayerSortType.Name:
                    this.PlayerListView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.PlayerName), ListSortDirection.Ascending));
                    break;

                case PlayerSortType.Online:
                    this.PlayerListView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.IsOnline), ListSortDirection.Descending));
                    this.PlayerListView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.PlayerName), ListSortDirection.Ascending));
                    break;

                case PlayerSortType.Tribe:
                    this.PlayerListView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.TribeName), ListSortDirection.Ascending));
                    this.PlayerListView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.PlayerName), ListSortDirection.Ascending));
                    break;

                case PlayerSortType.LastUpdated:
                    this.PlayerListView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.LastUpdated), ListSortDirection.Descending));
                    this.PlayerListView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.PlayerName), ListSortDirection.Ascending));
                    break;
            }
        }

        #region Filtering
        private void FilterPlayerList_Click(object sender, RoutedEventArgs e)
        {
            PlayerListView?.Refresh();
        }

        public bool PlayerListFilter(object obj)
        {
            var player = obj as PlayerInfo;
            if (player == null)
                return false;

            var result = (this.PlayerFiltering.HasFlag(PlayerFilterType.Online) && player.IsOnline) ||
                         (this.PlayerFiltering.HasFlag(PlayerFilterType.Offline) && !player.IsOnline) ||
                         (this.PlayerFiltering.HasFlag(PlayerFilterType.Admin) && player.IsAdmin) ||
                         (this.PlayerFiltering.HasFlag(PlayerFilterType.Whitelisted) && player.IsWhitelisted) ||
                         (this.PlayerFiltering.HasFlag(PlayerFilterType.Invalid) && !player.IsValid);
            if (!result)
                return false;

            var filterString = PlayerListFilterString.ToLower();

            if (string.IsNullOrWhiteSpace(filterString))
                return true;

            result = player.PlayerNameFilterString != null && player.PlayerNameFilterString.Contains(filterString) ||
                    player.TribeNameFilterString != null && player.TribeNameFilterString.Contains(filterString) ||
                    player.CharacterNameFilterString != null && player.CharacterNameFilterString.Contains(filterString);

            return result;
        }
        #endregion
    }
}
