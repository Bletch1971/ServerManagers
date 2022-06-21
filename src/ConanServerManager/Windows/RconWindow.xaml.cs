using ServerManagerTool.Common.Extensions;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using ServerManagerTool.Lib.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    public enum InputWindowMode
    {
        None,
        ServerChatTo,
        RenamePlayer,
        RenameTribe,
    }

    public class ScrollToBottomAction : TriggerAction<RichTextBox>
    {
        protected override void Invoke(object parameter)
        {
            AssociatedObject.ScrollToEnd();
        }
    }

    public class RconOutput_CommandTime : Run
    {
        public RconOutput_CommandTime()
            : this(DateTime.Now)
        {
        }

        public RconOutput_CommandTime(DateTime time)
            : base($"[{time.ToString("g")}] ")
        {
        }
    }

    public class RconOutput_TimedCommand : Span
    {
        protected RconOutput_TimedCommand()
            : base()
        {
            base.Inlines.Add(new RconOutput_CommandTime());
        }

        public RconOutput_TimedCommand(Inline output)
            : this()
        {
            base.Inlines.Add(output);
        }

        public RconOutput_TimedCommand(string output)
            : this(new Run(output))
        {
        }
    }

    public class RconOutput_Comment : Run
    {
        public RconOutput_Comment(string value)
            : base(value)
        {
            Foreground = Brushes.Green;
        }
    }

    public class RconOutput_ChatSend : RconOutput_TimedCommand
    {
        public RconOutput_ChatSend(string target, string output)
            : base($"[{target}] {output}")
        {
        }
    }

    public class RconOutput_Broadcast : RconOutput_ChatSend
    {
        public RconOutput_Broadcast(string output)
            : base("ALL", output)
        {
            Foreground = Brushes.Orange;
        }
    }

    public class RconOutput_ConnectionChanged : RconOutput_TimedCommand
    {
        public RconOutput_ConnectionChanged(bool isConnected)
            : base(isConnected ? GlobalizedApplication.Instance.GetResourceString("RCON_ConnectionEstablishedLabel") : GlobalizedApplication.Instance.GetResourceString("RCON_ConnectionLostLabel"))
        {
            Foreground = Brushes.Orange;
        }
    }

    public class RconOutput_Command : RconOutput_TimedCommand
    {
        public RconOutput_Command(string text)
            : base(text)
        {
        }
    };

    public class RconOutput_NoResponse : RconOutput_TimedCommand
    {
        public RconOutput_NoResponse()
            : base(GlobalizedApplication.Instance.GetResourceString("RCON_NoCommandResponseLabel"))
        {
            Foreground = Brushes.LightGray;
        }
    };

    public class RconOutput_CommandOutput : RconOutput_TimedCommand
    {
        public RconOutput_CommandOutput(string text)
            : base(text)
        {
        }
    };

    public class RconOutput_PlayerJoined : RconOutput_TimedCommand
    {
        public RconOutput_PlayerJoined(string text)
            : base(text)
        {
            Foreground = new SolidColorBrush(Color.FromArgb(255, 85, 142, 181));
        }
    };

    public class RconOutput_PlayerLeft : RconOutput_TimedCommand
    {
        public RconOutput_PlayerLeft(string text)
            : base(text)
        {
            Foreground = new SolidColorBrush(Color.FromArgb(255, 62, 104, 132));
        }
    };

    /// <summary>
    /// Interaction logic for RconWindow.xaml
    /// </summary>
    public partial class RconWindow : Window
    {
        private static Dictionary<Server, RconWindow> RconWindows = new Dictionary<Server, RconWindow>();

        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty ConfigProperty = DependencyProperty.Register(nameof(Config), typeof(Config), typeof(RconWindow), new PropertyMetadata(Config.Default));
        public static readonly DependencyProperty CurrentInputModeProperty = DependencyProperty.Register(nameof(CurrentInputMode), typeof(string), typeof(RconWindow), new PropertyMetadata(GameData.RCONINPUTMODE_COMMAND));
        public static readonly DependencyProperty PlayerFilteringProperty = DependencyProperty.Register(nameof(PlayerFiltering), typeof(PlayerFilterType), typeof(RconWindow), new PropertyMetadata(PlayerFilterType.Online | PlayerFilterType.Offline));
        public static readonly DependencyProperty PlayerFilterStringProperty = DependencyProperty.Register(nameof(PlayerFilterString), typeof(string), typeof(RconWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty PlayerSortingProperty = DependencyProperty.Register(nameof(PlayerSorting), typeof(PlayerSortType), typeof(RconWindow), new PropertyMetadata(PlayerSortType.Online));
        public static readonly DependencyProperty PlayersViewProperty = DependencyProperty.Register(nameof(PlayersView), typeof(ICollectionView), typeof(RconWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty RconParametersProperty = DependencyProperty.Register(nameof(RconParameters), typeof(RconParameters), typeof(RconWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty ScrollOnNewInputProperty = DependencyProperty.Register(nameof(ScrollOnNewInput), typeof(bool), typeof(RconWindow), new PropertyMetadata(true));
        public static readonly DependencyProperty ServerRconProperty = DependencyProperty.Register(nameof(ServerRcon), typeof(ServerRcon), typeof(RconWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty RconInputModesProperty = DependencyProperty.Register(nameof(RconInputModes), typeof(ComboBoxItemList), typeof(RconWindow), new PropertyMetadata(null));

        public RconWindow(RconParameters parameters)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            PopulateRconInputModesComboBox();

            this.CurrentInputWindowMode = InputWindowMode.None;
            this.PlayerFiltering = (PlayerFilterType)Config.Default.RCON_PlayerListFilter;
            this.PlayerSorting = (PlayerSortType)Config.Default.RCON_PlayerListSort;
            this.RconParameters = parameters;
            this.ServerRcon = new ServerRcon(parameters);
            this.ServerRcon.RegisterCommandListener(RenderRCONCommandOutput);
            this.ServerRcon.Players.CollectionChanged += Players_CollectionChanged;
            this.ServerRcon.PlayersCollectionUpdated += Players_CollectionUpdated;

            this.PlayersView = CollectionViewSource.GetDefaultView(this.ServerRcon.Players);
            this.PlayersView.Filter = new Predicate<object>(PlayerFilter);

            if (this.RconParameters?.Server?.Runtime != null)
            {
                this.RconParameters.Server.Runtime.StatusUpdate += Runtime_StatusUpdate;
            }

            if (this.RconParameters?.Server == null)
            {
                this.PlayerCountSeparator.Visibility = Visibility.Collapsed;
                this.MaxPlayerLabel.Visibility = Visibility.Collapsed;
            }

            this.ServerRcon.Initialize();

            this.DataContext = this;

            RenderCommentsBlock(
                _globalizer.GetResourceString("RCON_Comments_Line1"),
                _globalizer.GetResourceString("RCON_Comments_Line2"),
                _globalizer.GetResourceString("RCON_Comments_Line3"),
                _globalizer.GetResourceString("RCON_Comments_Line4"),
                _globalizer.GetResourceString("RCON_Comments_Line5")
            );

            if (this.RconParameters.WindowExtents.Width > 50 && this.RconParameters.WindowExtents.Height > 50)
            {
                this.Left = this.RconParameters.WindowExtents.Left;
                this.Top = this.RconParameters.WindowExtents.Top;
                this.Width = this.RconParameters.WindowExtents.Width;
                this.Height = this.RconParameters.WindowExtents.Height;

                //
                // Fix issues where the console was saved while offscreen.
                if(this.Left == -32000)
                {
                    this.Left = 0;
                }

                if(this.Top == -32000)
                {
                    this.Top = 0;
                }
            }

            SetPlayerListWidth(this.RconParameters.PlayerListWidth);

            // hook into the language change event
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent += ResourceDictionaryChangedEvent;
            GameData.GameDataLoaded += GameData_GameDataLoaded;
        }

        #region Properties
        public Config Config
        {
            get { return (Config)GetValue(ConfigProperty); }
            set { SetValue(ConfigProperty, value); }
        }

        public string CurrentInputMode
        {
            get { return (string)GetValue(CurrentInputModeProperty); }
            set { SetValue(CurrentInputModeProperty, value); }
        }

        public PlayerFilterType PlayerFiltering
        {
            get { return (PlayerFilterType)GetValue(PlayerFilteringProperty); }
            set { SetValue(PlayerFilteringProperty, value); }
        }

        public string PlayerFilterString
        {
            get { return (string)GetValue(PlayerFilterStringProperty); }
            set { SetValue(PlayerFilterStringProperty, value); }
        }

        public PlayerSortType PlayerSorting
        {
            get { return (PlayerSortType)GetValue(PlayerSortingProperty); }
            set { SetValue(PlayerSortingProperty, value); }
        }

        public ICollectionView PlayersView
        {
            get { return (ICollectionView)GetValue(PlayersViewProperty); }
            set { SetValue(PlayersViewProperty, value); }
        }

        public RconParameters RconParameters
        {
            get { return (RconParameters)GetValue(RconParametersProperty); }
            set { SetValue(RconParametersProperty, value); }
        }

        public bool ScrollOnNewInput
        {
            get { return (bool)GetValue(ScrollOnNewInputProperty); }
            set { SetValue(ScrollOnNewInputProperty, value); }
        }

        public ServerRcon ServerRcon
        {
            get { return (ServerRcon)GetValue(ServerRconProperty); }
            set { SetValue(ServerRconProperty, value); }
        }

        public ComboBoxItemList RconInputModes
        {
            get { return (ComboBoxItemList)GetValue(RconInputModesProperty); }
            set { SetValue(RconInputModesProperty, value); }
        }
        #endregion

        #region Commands
        private InputWindowMode CurrentInputWindowMode
        {
            get;
            set;
        }

        public ICommand Button1Command
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        inputBox.Visibility = Visibility.Collapsed;
                        dockPanel.IsEnabled = true;

                        var inputText = inputTextBox.Text;

                        switch (this.CurrentInputWindowMode)
                        {
                            default:
                                break;
                        }

                        // Clear InputBox.
                        inputTextBox.Text = String.Empty;
                        this.CurrentInputWindowMode = InputWindowMode.None;
                    },
                    canExecute: (_) => true
                );
            }
        }

        public ICommand Button2Command
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        inputBox.Visibility = Visibility.Collapsed;
                        dockPanel.IsEnabled = true;

                        switch (this.CurrentInputWindowMode)
                        {
                            default:
                                break;
                        }

                        // Clear InputBox.
                        inputTextBox.Text = String.Empty;
                        this.CurrentInputWindowMode = InputWindowMode.None;
                    },
                    canExecute: (_) => true
                );
            }
        }

        public ICommand ClearLogsCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        string logsDir = String.Empty;
                        try
                        {
                            logsDir = App.GetProfileLogFolder(this.RconParameters.ProfileId);
                            Directory.Delete(logsDir, true);
                        }
                        catch (Exception)
                        {
                            // Ignore any failures here, best effort only.
                        }

                        MessageBox.Show(String.Format(_globalizer.GetResourceString("RCON_ClearLogs_Label"), logsDir), _globalizer.GetResourceString("RCON_ClearLogs_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                    },
                    canExecute: (_) => this.RconParameters.Server != null
                );
            }
        }

        public ICommand ViewLogsCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        string logsDir = String.Empty;
                        try
                        {
                            logsDir = App.GetProfileLogFolder(this.RconParameters.ProfileId);
                            Process.Start(logsDir);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(String.Format(_globalizer.GetResourceString("RCON_ViewLogs_ErrorLabel"), logsDir, ex.Message), _globalizer.GetResourceString("RCON_ViewLogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    },
                    canExecute: (_) => this.RconParameters.Server != null
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
                        Config.Default.RCON_PlayerListSort = (int)this.PlayerSorting;
                        SortPlayers();
                    },
                    canExecute: (sort) => true
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
                        Config.Default.RCON_PlayerListFilter = (int)this.PlayerFiltering;
                        this.PlayersView.Refresh();
                    },
                    canExecute: (filter) => true
                );
            }
        }

        public ICommand ViewPlayerProfileCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) =>
                    {
                        var savedFilesPath = ServerProfile.GetProfileSavePath(this.RconParameters.InstallDirectory);
                        var window = new PlayerProfileWindow(player, savedFilesPath)
                        {
                            Owner = this
                        };
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
                    execute: (player) =>
                    {
                        var savedFilesPath = ServerProfile.GetProfileSavePath(this.RconParameters.InstallDirectory);
                        var window = new GuildProfileWindow(player, this.ServerRcon.Players, savedFilesPath)
                        {
                            Owner = this
                        };
                        window.ShowDialog();
                    },
                    canExecute: (player) => player?.PlayerData != null && !string.IsNullOrWhiteSpace(player.GuildName)
                    );
            }
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
                                MessageBox.Show($"{_globalizer.GetResourceString("RCON_CopyPlayerIdLabel")} {player.CharacterName}", _globalizer.GetResourceString("RCON_CopyPlayerIdTitle"), MessageBoxButton.OK);
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
        #endregion

        #region Events
        public static RconWindow GetRconForServer(Server server)
        {
            if (!RconWindows.TryGetValue(server, out RconWindow window) || !window.IsLoaded)
            {
                window = new RconWindow(new RconParameters()
                {
                    WindowTitle = String.Format(GlobalizedApplication.Instance.GetResourceString("RCON_TitleLabel"), server.Runtime.ProfileSnapshot.ProfileName),
                    WindowExtents = server.Profile.RconWindowExtents,
                    PlayerListWidth = server.Profile.RconPlayerListWidth,

                    Server = server,
                    InstallDirectory = server.Runtime.ProfileSnapshot.InstallDirectory,
                    GameFile = server.Runtime.ProfileSnapshot.GameFile,
                    ProfileId = server.Runtime.ProfileSnapshot.ProfileId,
                    ProfileName = server.Runtime.ProfileSnapshot.ProfileName,
                    MaxPlayers = server.Runtime.MaxPlayers,
                    RconHost = server.Runtime.ProfileSnapshot.ServerIPAddress.ToString(),
                    RconPort = server.Runtime.ProfileSnapshot.RconPort,
                    RconPassword = server.Runtime.ProfileSnapshot.RconPassword,
                });
                RconWindows[server] = window;
            }

            return window;
        }

        private void GameData_GameDataLoaded(object sender, EventArgs e)
        {
            PopulateRconInputModesComboBox();
        }

        private void ResourceDictionaryChangedEvent(object source, ResourceDictionaryChangedEventArgs e)
        {
            GameData_GameDataLoaded(source, e);
        }

        protected override void OnClosed(EventArgs e)
        {
            GameData.GameDataLoaded -= GameData_GameDataLoaded;
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent -= ResourceDictionaryChangedEvent;

            if (this.RconParameters?.Server?.Runtime != null)
            {
                this.RconParameters.Server.Runtime.StatusUpdate -= Runtime_StatusUpdate;
            }

            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.ServerRcon.PlayersCollectionUpdated -= Players_CollectionUpdated;
            this.ServerRcon.Players.CollectionChanged -= Players_CollectionChanged;
            this.ServerRcon.DisposeAsync().DoNotWait();

            if (this.RconParameters?.Server != null)
            {
                RconWindows.TryGetValue(this.RconParameters.Server, out RconWindow window);
                if (window != null)
                    RconWindows.Remove(this.RconParameters.Server);
            }

            base.OnClosing(e);
        }

        private void RCON_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                Rect savedRect = this.RconParameters.WindowExtents;
                this.RconParameters.WindowExtents = new Rect(savedRect.Location, e.NewSize);
                if (this.RconParameters.Server != null)
                {
                    this.RconParameters.Server.Profile.RconWindowExtents = this.RconParameters.WindowExtents;
                }
            }
        }

        private void RCON_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized && this.Left != -32000 && this.Top != -32000)
            {
                Rect savedRect = this.RconParameters.WindowExtents;
                this.RconParameters.WindowExtents = new Rect(new Point(this.Left, this.Top), savedRect.Size);
                if (this.RconParameters.Server != null)
                {
                    this.RconParameters.Server.Profile.RconWindowExtents = this.RconParameters.WindowExtents;
                }
            }
        }

        private void ConsoleInput_KeyUp(object sender, KeyEventArgs e)
        {            
            if(e.Key == Key.Enter)
            {
                var textBox = (TextBox)sender;
                var effectiveMode = this.CurrentInputMode;
                var commandText = textBox.Text.Trim();

                if (commandText.StartsWith("/"))
                {
                    effectiveMode = GameData.RCONINPUTMODE_COMMAND;
                    commandText = commandText.Substring(1);
                }

                switch (effectiveMode)
                {
                    case GameData.RCONINPUTMODE_COMMAND:
                        this.ServerRcon.IssueCommand(commandText);
                        break;

                    default:
                        this.ServerRcon.IssueCommand($"{effectiveMode} {commandText}");
                        break;
                }

                textBox.Text = String.Empty;
            }
        }

        private void Players_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SortPlayers();
            PlayersView?.Refresh();
        }

        private void Players_CollectionUpdated(object sender, EventArgs e)
        {
            this.PlayersView = CollectionViewSource.GetDefaultView(this.ServerRcon.Players);
            this.PlayersView.Filter = new Predicate<object>(PlayerFilter);

            SortPlayers();
            PlayersView?.Refresh();
        }

        private void PlayerList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.RconParameters.PlayerListWidth = this.playerListColumn.Width.Value;
            if (this.RconParameters.Server != null)
            {
                this.RconParameters.Server.Profile.RconPlayerListWidth = this.RconParameters.PlayerListWidth;
            }
        }

        private void Runtime_StatusUpdate(object sender, EventArgs eventArgs)
        {
            this.RconParameters.ProfileName = this.RconParameters?.Server?.Runtime?.ProfileSnapshot.ProfileName ?? "<unknown>";
            this.RconParameters.MaxPlayers = this.RconParameters?.Server?.Runtime?.MaxPlayers ?? 0;
        }
        #endregion

        #region Methods
        public static void CloseAllWindows()
        {
            var windows = RconWindows.Values.ToList();
            foreach (var window in windows)
            {
                if (window.IsLoaded)
                    window.Close();
            }
            windows.Clear();
        }

        private void SetPlayerListWidth(double value)
        {
            if (value < this.playerListColumn.MinWidth)
                this.playerListColumn.Width = new GridLength(this.playerListColumn.MinWidth, GridUnitType.Pixel);
            else
                this.playerListColumn.Width = new GridLength(value, GridUnitType.Pixel);
        }

        private void PopulateRconInputModesComboBox()
        {
            var selectedValue = this.InputModesComboBox?.SelectedValue ?? GameData.RCONINPUTMODE_COMMAND;
            var list = new ComboBoxItemList();

            foreach (var item in GameData.GetAllRconInputModes())
            {
                item.DisplayMember = GameData.FriendlyRconInputModeName(item.ValueMember);
                list.Add(item);
            }

            this.RconInputModes = list;
            if (this.InputModesComboBox != null)
            {
                this.InputModesComboBox.SelectedValue = selectedValue;
            }
        }

        public void SortPlayers()
        {
            this.PlayersView.SortDescriptions.Clear();

            switch (this.PlayerSorting)
            {
                case PlayerSortType.Name:
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.PlayerName), ListSortDirection.Ascending));
                    break;

                case PlayerSortType.Online:
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.IsOnline), ListSortDirection.Descending));
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.PlayerName), ListSortDirection.Ascending));
                    break;

                case PlayerSortType.Tribe:
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.GuildName), ListSortDirection.Ascending));
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.PlayerName), ListSortDirection.Ascending));
                    break;

                case PlayerSortType.LastOnline:
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.LastOnline), ListSortDirection.Descending));
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.PlayerName), ListSortDirection.Ascending));
                    break;
            }
        }
        #endregion

        #region Command Methods
        private void AddBlockContent(Block b)
        {
            ConsoleContent.Blocks.Add(b);            
        }

        private IEnumerable<Inline> FormatCommandInput(ConsoleCommand command)
        {
            var commandValue = command?.command?.ToLower() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(commandValue))
            {
                yield return new LineBreak();
            }

            var found = false;
            foreach (var item in GameData.GetMessageRconInputModes())
            {
                if (item.ValueMember.Equals(commandValue, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    yield return new RconOutput_Broadcast(command.args);
                }
            }

            if (!found)
            {
                yield return new RconOutput_Command($"> {command.rawCommand}");
            }

            if (!command.suppressOutput && !command.lines.IsEmpty())
            {
                yield return new LineBreak();
            }
        }

        private IEnumerable<Inline> FormatCommandOutput(ConsoleCommand command)
        {
            bool firstLine = true;

            if (command.command.Equals(ServerRcon.RCON_COMMAND_LISTPLAYERS, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var output in command.lines)
                {
                    var trimmed = output.TrimEnd();

                    if (!firstLine)
                    {
                        yield return new LineBreak();
                    }
                    firstLine = false;

                    if (trimmed == ServerRcon.NoResponseOutput)
                    {
                        yield return new RconOutput_NoResponse();
                    }
                    else if (trimmed.EndsWith("joined the game."))
                    {
                        yield return new RconOutput_PlayerJoined(trimmed);
                    }
                    else if (trimmed.EndsWith("left the game."))
                    {
                        yield return new RconOutput_PlayerLeft(trimmed);
                    }
                    else
                    {
                        yield return new RconOutput_CommandOutput(trimmed);
                    }
                }
            }
            else
            {
                foreach (var output in command.lines)
                {
                    var trimmed = output.TrimEnd();

                    if (!firstLine)
                    {
                        yield return new LineBreak();
                    }
                    firstLine = false;

                    if (trimmed == ServerRcon.NoResponseOutput)
                    {
                        yield return new RconOutput_NoResponse();
                    }
                    else
                    {
                        yield return new RconOutput_CommandOutput(trimmed);
                    }
                }
            }
        }

        private void RenderCommentsBlock(params string[] lines)
        {
            var p = new Paragraph();
            bool firstLine = true;

            foreach (var output in lines)
            {
                var trimmed = output.TrimEnd();

                if (!firstLine)
                {
                    p.Inlines.Add(new LineBreak());
                }
                firstLine = false;

                p.Inlines.Add(new RconOutput_Comment(trimmed));
            }

            AddBlockContent(p);
        }

        private void RenderRCONCommandOutput(ConsoleCommand command)
        {
            //
            // Format output
            //
            var p = new Paragraph();

            if (!command.suppressCommand)
            {
                foreach (var element in FormatCommandInput(command))
                {
                    p.Inlines.Add(element);
                }
            }

            if (!command.suppressOutput)
            {
                foreach (var element in FormatCommandOutput(command))
                {
                    p.Inlines.Add(element);
                }
            }

            if (p.Inlines.Count > 0)
            {
                AddBlockContent(p);
            }
        }
        #endregion

        #region Filtering
        private void FilterPlayerList_Click(object sender, RoutedEventArgs e)
        {
            PlayersView?.Refresh();
        }

        public bool PlayerFilter(object obj)
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

            var filterString = PlayerFilterString.ToLower();

            if (string.IsNullOrWhiteSpace(filterString))
                return true;

            result = player.PlatformNameFilterString != null && player.PlatformNameFilterString.Contains(filterString) ||
                    player.GuildNameFilterString != null && player.GuildNameFilterString.Contains(filterString) ||
                    player.CharacterNameFilterString != null && player.CharacterNameFilterString.Contains(filterString);

            return result;
        }
        #endregion
    }
}
