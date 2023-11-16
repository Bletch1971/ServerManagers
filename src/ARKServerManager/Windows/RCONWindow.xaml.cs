using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Extensions;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using ServerManagerTool.Lib.ViewModel.RCON;
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

    public class RCONOutput_CommandTime : Run
    {
        public RCONOutput_CommandTime() 
            : this(DateTime.Now)
        {
        }

        public RCONOutput_CommandTime(DateTime time) 
            : base($"[{time.ToString("g")}] ")
        {
        }
    }

    public class RCONOutput_TimedCommand : Span
    {
        protected RCONOutput_TimedCommand() 
            : base()
        {
            base.Inlines.Add(new RCONOutput_CommandTime());
        }

        public RCONOutput_TimedCommand(Inline output) 
            : this()
        {            
            base.Inlines.Add(output);
        }

        public RCONOutput_TimedCommand(string output) 
            : this(new Run(output))
        {
        }
    }

    public class RCONOutput_Comment : Run
    {
        public RCONOutput_Comment(string value) 
            : base(value)
        {
            Foreground = Brushes.Green;
        }
    }

    public class RCONOutput_ChatSend : RCONOutput_TimedCommand
    {
        public RCONOutput_ChatSend(string target, string output) 
            : base($"[{target}] {output}")
        {
        }
    }

    public class RCONOutput_Broadcast : RCONOutput_ChatSend
    {
        public RCONOutput_Broadcast(string output) 
            : base("ALL", output)
        {
            Foreground = Brushes.Orange;
        }
    }

    public class RCONOutput_ConnectionChanged : RCONOutput_TimedCommand
    {
        public RCONOutput_ConnectionChanged(bool isConnected) 
            : base(isConnected ? GlobalizedApplication.Instance.GetResourceString("RCON_ConnectionEstablishedLabel") : GlobalizedApplication.Instance.GetResourceString("RCON_ConnectionLostLabel"))
        {
            Foreground = Brushes.Orange;
        }
    }

    public class RCONOutput_Command : RCONOutput_TimedCommand
    {
        public RCONOutput_Command(string text) 
            : base(text)
        {
        }
    };

    public class RCONOutput_NoResponse : RCONOutput_TimedCommand
    {
        public RCONOutput_NoResponse() 
            : base(GlobalizedApplication.Instance.GetResourceString("RCON_NoCommandResponseLabel"))
        {
            Foreground = Brushes.LightGray;
        }
    };

    public class RCONOutput_CommandOutput : RCONOutput_TimedCommand
    {
        public RCONOutput_CommandOutput(string text) 
            : base(text)
        {
        }
    };

    public class RCONOutput_PlayerJoined : RCONOutput_TimedCommand
    {
        public RCONOutput_PlayerJoined(string text)
            : base(text)
        {
            Foreground = new SolidColorBrush(Color.FromArgb(255, 85, 142, 181));
        }
    };

    public class RCONOutput_PlayerLeft : RCONOutput_TimedCommand
    {
        public RCONOutput_PlayerLeft(string text)
            : base(text)
        {
            Foreground = new SolidColorBrush(Color.FromArgb(255, 62, 104, 132));
        }
    };

    /// <summary>
    /// Interaction logic for RCON.xaml
    /// </summary>
    public partial class RCONWindow : Window
    {
        private static Dictionary<Server, RCONWindow> RCONWindows = new Dictionary<Server, RCONWindow>();

        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty CurrentConfigProperty = DependencyProperty.Register(nameof(CurrentConfig), typeof(Config), typeof(RCONWindow), new PropertyMetadata(Config.Default));
        public static readonly DependencyProperty CurrentInputModeProperty = DependencyProperty.Register(nameof(CurrentInputMode), typeof(string), typeof(RCONWindow), new PropertyMetadata(GameData.RCONINPUTMODE_COMMAND));
        public static readonly DependencyProperty PlayerFilteringProperty = DependencyProperty.Register(nameof(PlayerFiltering), typeof(PlayerFilterType), typeof(RCONWindow), new PropertyMetadata(PlayerFilterType.Online | PlayerFilterType.Offline | PlayerFilterType.Whitelisted));
        public static readonly DependencyProperty PlayerFilterStringProperty = DependencyProperty.Register(nameof(PlayerFilterString), typeof(string), typeof(RCONWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty PlayerSortingProperty = DependencyProperty.Register(nameof(PlayerSorting), typeof(PlayerSortType), typeof(RCONWindow), new PropertyMetadata(PlayerSortType.Online));
        public static readonly DependencyProperty PlayersViewProperty = DependencyProperty.Register(nameof(PlayersView), typeof(ICollectionView), typeof(RCONWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty RCONParametersProperty = DependencyProperty.Register(nameof(RCONParameters), typeof(RCONParameters), typeof(RCONWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty ScrollOnNewInputProperty = DependencyProperty.Register(nameof(ScrollOnNewInput), typeof(bool), typeof(RCONWindow), new PropertyMetadata(true));
        public static readonly DependencyProperty ServerRCONProperty = DependencyProperty.Register(nameof(ServerRCON), typeof(ServerRcon), typeof(RCONWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty RconInputModesProperty = DependencyProperty.Register(nameof(RconInputModes), typeof(ComboBoxItemList), typeof(RCONWindow), new PropertyMetadata(null));

        public RCONWindow(RCONParameters parameters)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            PopulateRconInputModesComboBox();

            this.CurrentInputWindowMode = InputWindowMode.None;
            this.PlayerFiltering = (PlayerFilterType)Config.Default.RCON_PlayerListFilter;
            this.PlayerSorting = (PlayerSortType)Config.Default.RCON_PlayerListSort;
            this.RCONParameters = parameters;
            this.ServerRCON = new ServerRcon(parameters);
            this.ServerRCON.RegisterCommandListener(RenderRCONCommandOutput);
            this.ServerRCON.Players.CollectionChanged += Players_CollectionChanged;
            this.ServerRCON.PlayersCollectionUpdated += Players_CollectionUpdated;

            this.PlayersView = CollectionViewSource.GetDefaultView(this.ServerRCON.Players);
            this.PlayersView.Filter = new Predicate<object>(PlayerFilter);

            var notifier = new PropertyChangeNotifier(this.ServerRCON, ServerRcon.StatusProperty, (o, e) =>
            {
                this.RenderConnectionStateChange(e);
            });

            if (this.RCONParameters?.Server?.Runtime != null)
            {
                this.RCONParameters.Server.Runtime.StatusUpdate += Runtime_StatusUpdate;
            }

            if (this.RCONParameters?.Server == null)
            {
                this.PlayerCountSeparator.Visibility = Visibility.Collapsed;
                this.MaxPlayerLabel.Visibility = Visibility.Collapsed;
            }

            this.ServerRCON.Initialize();

            this.DataContext = this;

            RenderCommentsBlock(
                _globalizer.GetResourceString("RCON_Comments_Line1"),
                _globalizer.GetResourceString("RCON_Comments_Line2"),
                _globalizer.GetResourceString("RCON_Comments_Line3"),
                _globalizer.GetResourceString("RCON_Comments_Line4"),
                _globalizer.GetResourceString("RCON_Comments_Line5"),
                String.Format(_globalizer.GetResourceString("RCON_Comments_Line6"), 
                _globalizer.GetResourceString("RCON_Help_Keyword")));

            if (this.RCONParameters.WindowExtents.Width > 50 && this.RCONParameters.WindowExtents.Height > 50)
            {
                this.Left = this.RCONParameters.WindowExtents.Left;
                this.Top = this.RCONParameters.WindowExtents.Top;
                this.Width = this.RCONParameters.WindowExtents.Width;
                this.Height = this.RCONParameters.WindowExtents.Height;

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

            SetPlayerListWidth(this.RCONParameters.PlayerListWidth);

            // hook into the language change event
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent += ResourceDictionaryChangedEvent;
            GameData.GameDataLoaded += GameData_GameDataLoaded;
        }

        #region Properties
        public Config CurrentConfig
        {
            get { return (Config)GetValue(CurrentConfigProperty); }
            set { SetValue(CurrentConfigProperty, value); }
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

        public RCONParameters RCONParameters
        {
            get { return (RCONParameters)GetValue(RCONParametersProperty); }
            set { SetValue(RCONParametersProperty, value); }
        }

        public bool ScrollOnNewInput
        {
            get { return (bool)GetValue(ScrollOnNewInputProperty); }
            set { SetValue(ScrollOnNewInputProperty, value); }
        }

        public ServerRcon ServerRCON
        {
            get { return (ServerRcon)GetValue(ServerRCONProperty); }
            set { SetValue(ServerRCONProperty, value); }
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

                        PlayerInfo player;
                        var inputText = inputTextBox.Text;

                        switch (this.CurrentInputWindowMode)
                        {
                            case InputWindowMode.ServerChatTo:
                                player = inputBox.Tag as PlayerInfo;
                                if (player != null)
                                    this.ServerRCON.IssueCommand($"ServerChatTo \"{player.PlayerId}\" {inputText}");
                                break;

                            case InputWindowMode.RenamePlayer:
                                player = inputBox.Tag as PlayerInfo;
                                if (player != null && player.PlayerData != null)
                                    this.ServerRCON.IssueCommand($"RenamePlayer \"{player.CharacterName}\" {inputText}");
                                break;

                            case InputWindowMode.RenameTribe:
                                player = inputBox.Tag as PlayerInfo;
                                if (player != null)
                                    this.ServerRCON.IssueCommand($"RenameTribe \"{player.TribeName}\" {inputText}");
                                break;

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
                            logsDir = App.GetProfileLogFolder(this.RCONParameters.ProfileId);
                            Directory.Delete(logsDir, true);
                        }
                        catch (Exception)
                        {
                            // Ignore any failures here, best effort only.
                        }

                        MessageBox.Show(String.Format(_globalizer.GetResourceString("RCON_ClearLogs_Label"), logsDir), _globalizer.GetResourceString("RCON_ClearLogs_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                    },
                    canExecute: (_) => this.RCONParameters.Server != null
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
                            logsDir = App.GetProfileLogFolder(this.RCONParameters.ProfileId);
                            Process.Start(logsDir);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(String.Format(_globalizer.GetResourceString("RCON_ViewLogs_ErrorLabel"), logsDir, ex.Message), _globalizer.GetResourceString("RCON_ViewLogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    },
                    canExecute: (_) => this.RCONParameters.Server != null
                );
            }
        }

        public ICommand SaveWorldCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        var message = _globalizer.GetResourceString("RCON_SaveWorldLabel");
                        this.ServerRCON.IssueCommand($"{Config.Default.RCON_MessageCommand.ToLower()} {message}");

                        this.ServerRCON.IssueCommand(Config.Default.ServerSaveCommand);
                    },
                    canExecute: (_) => true
                );
            }
        }

        public ICommand DestroyWildDinosCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        var message = _globalizer.GetResourceString("RCON_DestroyWildDinosLabel");
                        this.ServerRCON.IssueCommand($"{Config.Default.RCON_MessageCommand.ToLower()} {message}");

                        this.ServerRCON.IssueCommand(ServerRcon.RCON_COMMAND_WILDDINOWIPE);
                    },
                    canExecute: (_) => true
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

        public ICommand ChatPlayerCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => 
                    {
                        dockPanel.IsEnabled = false;

                        CurrentInputWindowMode = InputWindowMode.ServerChatTo;
                        inputBox.Tag = player;
                        inputTitle.Text = $"{_globalizer.GetResourceString("RCON_ChatPlayerLabel")} {player.PlayerName ?? _globalizer.GetResourceString("RCON_UnnamedLabel")}";
                        inputTextBox.Text = string.Empty;
                        button1.Content = _globalizer.GetResourceString("RCON_Button_Send");
                        button2.Content = _globalizer.GetResourceString("RCON_Button_Cancel");
                        inputBox.Visibility = Visibility.Visible;
                    },
                    canExecute: (player) => player != null && player.IsValid && player.IsOnline
                );
            }
        }

        public ICommand RenamePlayerCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => 
                    {
                        dockPanel.IsEnabled = false;

                        CurrentInputWindowMode = InputWindowMode.RenamePlayer;
                        inputBox.Tag = player;
                        inputTitle.Text = $"{_globalizer.GetResourceString("RCON_RenamePlayerLabel")} {player.CharacterName ?? _globalizer.GetResourceString("RCON_UnnamedLabel")}";
                        inputTextBox.Text = string.Empty;
                        button1.Content = _globalizer.GetResourceString("RCON_Button_Change");
                        button2.Content = _globalizer.GetResourceString("RCON_Button_Cancel");
                        inputBox.Visibility = System.Windows.Visibility.Visible;
                    },
                    canExecute: (player) => player?.PlayerData != null && player.IsValid && player.IsOnline
                );
            }
        }

        public ICommand RenameTribeCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => 
                    {
                        dockPanel.IsEnabled = false;

                        CurrentInputWindowMode = InputWindowMode.RenameTribe;
                        inputBox.Tag = player;
                        inputTitle.Text = $"{_globalizer.GetResourceString("RCON_RenameTribeLabel")} {player.TribeName ?? _globalizer.GetResourceString("RCON_UnnamedLabel")}";
                        inputTextBox.Text = string.Empty;
                        button1.Content = _globalizer.GetResourceString("RCON_Button_Change");
                        button2.Content = _globalizer.GetResourceString("RCON_Button_Cancel");
                        inputBox.Visibility = System.Windows.Visibility.Visible;
                    },
                    canExecute: (player) => player != null && player.IsValid && player.IsOnline
                );
            }
        }

        public ICommand ViewPlayerProfileCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => {
                        var savedArksPath = ServerProfile.GetProfileSavePath(this.RCONParameters.InstallDirectory, this.RCONParameters.AltSaveDirectoryName, this.RCONParameters.PGM_Enabled, this.RCONParameters.PGM_Name);
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
                        var savedArksPath = ServerProfile.GetProfileSavePath(this.RCONParameters.InstallDirectory, this.RCONParameters.AltSaveDirectoryName, this.RCONParameters.PGM_Enabled, this.RCONParameters.PGM_Name);
                        var window = new TribeProfileWindow(player, this.ServerRCON.Players, savedArksPath);
                        window.Owner = this;
                        window.ShowDialog();
                    },
                    canExecute: (player) => player?.PlayerData != null && !string.IsNullOrWhiteSpace(player.TribeName)
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

        public ICommand KickPlayerProfileCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) =>
                    {
                        if (player.PlayerData != null)
                        {
                            this.ServerRCON.IssueCommand($"{ServerRcon.RCON_COMMAND_KICKPLAYER} {player.PlayerId.ToString()}");
                        }
                    },
                    canExecute: (player) => player != null
                );
            }
        }

        public ICommand BanPlayerProfileCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) =>
                    {
                        if (player.PlayerData != null)
                        {
                            this.ServerRCON.IssueCommand($"{ServerRcon.RCON_COMMAND_BANPLAYER} {player.PlayerId.ToString()}");
                        }
                    },
                    canExecute: (player) => player != null
                );
            }
        }

        public ICommand UnbanPlayerProfileCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) =>
                    {
                        if (player.PlayerData != null)
                        {
                            this.ServerRCON.IssueCommand($"{ServerRcon.RCON_COMMAND_UNBANPLAYER} {player.PlayerId.ToString()}");
                        }
                    },
                    canExecute: (player) => player != null
                );
            }
        }
        #endregion

        #region Events
        public static RCONWindow GetRCONForServer(Server server)
        {
            RCONWindow window;
            if (!RCONWindows.TryGetValue(server, out window) || !window.IsLoaded)
            {
                window = new RCONWindow(new RCONParameters()
                {
                    WindowTitle = String.Format(GlobalizedApplication.Instance.GetResourceString("RCON_TitleLabel"), server.Runtime.ProfileSnapshot.ProfileName),
                    WindowExtents = server.Profile.RCONWindowExtents,
                    PlayerListWidth = server.Profile.RCONPlayerListWidth,

                    Server = server,
                    InstallDirectory = server.Runtime.ProfileSnapshot.InstallDirectory,
                    AltSaveDirectoryName = server.Runtime.ProfileSnapshot.AltSaveDirectoryName,
                    ProfileId = server.Runtime.ProfileSnapshot.ProfileId,
                    ProfileName = server.Runtime.ProfileSnapshot.ProfileName,
                    MaxPlayers = server.Runtime.MaxPlayers,
                    RCONHost = server.Runtime.ProfileSnapshot.ServerIPAddress.ToString(),
                    RCONPort = server.Runtime.ProfileSnapshot.RCONPort,
                    RCONPassword = server.Runtime.ProfileSnapshot.RCONPassword,

                    PGM_Enabled = server.Profile.PGM_Enabled,
                    PGM_Name = server.Profile.PGM_Name,
                });
                RCONWindows[server] = window;
            }

            return window;
        }

        public static RCONWindow GetRCON(RCONParameters parameters)
        {
            return new RCONWindow(parameters);
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

            if (this.RCONParameters?.Server?.Runtime != null)
            {
                this.RCONParameters.Server.Runtime.StatusUpdate -= Runtime_StatusUpdate;
            }

            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.ServerRCON.PlayersCollectionUpdated -= Players_CollectionUpdated;
            this.ServerRCON.Players.CollectionChanged -= Players_CollectionChanged;
            this.ServerRCON.DisposeAsync().DoNotWait();

            if (this.RCONParameters?.Server != null)
            {
                RCONWindows.TryGetValue(this.RCONParameters.Server, out RCONWindow window);
                if (window != null)
                    RCONWindows.Remove(this.RCONParameters.Server);
            }

            base.OnClosing(e);
        }

        private void RCON_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                Rect savedRect = this.RCONParameters.WindowExtents;
                this.RCONParameters.WindowExtents = new Rect(savedRect.Location, e.NewSize);
                if (this.RCONParameters.Server != null)
                {
                    this.RCONParameters.Server.Profile.RCONWindowExtents = this.RCONParameters.WindowExtents;
                }
            }
        }

        private void RCON_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized && this.Left != -32000 && this.Top != -32000)
            {
                Rect savedRect = this.RCONParameters.WindowExtents;
                this.RCONParameters.WindowExtents = new Rect(new Point(this.Left, this.Top), savedRect.Size);
                if (this.RCONParameters.Server != null)
                {
                    this.RCONParameters.Server.Profile.RCONWindowExtents = this.RCONParameters.WindowExtents;
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
                if (commandText.StartsWith(_globalizer.GetResourceString("RCON_Help_Keyword")))
                {
                    RenderCommentsBlock(
                        _globalizer.GetResourceString("RCON_Help_Line1"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line2"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line3"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line4"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line5"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line6"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line7"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line8"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line9"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line10"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line11"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line12"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line13"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line14"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line15"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line16"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line17"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line18"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line19"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line20"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line21"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line22"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line23"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line24"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line25"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line26"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line27"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line28"),
                        _globalizer.GetResourceString("RCON_Help_Line29"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line30"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line31"),
                        "   " + _globalizer.GetResourceString("RCON_Help_Line32")
                        );
                }
                else
                {
                    if (commandText.StartsWith("/"))
                    {
                        effectiveMode = GameData.RCONINPUTMODE_COMMAND;
                        commandText = commandText.Substring(1);
                    }

                    switch (effectiveMode)
                    {
                        case GameData.RCONINPUTMODE_COMMAND:
                            this.ServerRCON.IssueCommand(commandText);
                            break;

                        case "Global":
                            if (string.IsNullOrWhiteSpace(Config.Default.RCON_AdminName))
                            {
                                this.ServerRCON.IssueCommand($"{ServerRcon.RCON_COMMAND_SERVERCHAT} {commandText}");
                            }
                            else
                            {
                                this.ServerRCON.IssueCommand($"{ServerRcon.RCON_COMMAND_SERVERCHAT} [{Config.Default.RCON_AdminName}] {commandText}");
                            }
                            break;

                        default:
                            this.ServerRCON.IssueCommand($"{effectiveMode} {commandText}");
                            break;
                    }
                }

                textBox.Text = String.Empty;
            }
        }

        private void Players_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SortPlayers();
            this.PlayersView?.Refresh();
        }

        private void Players_CollectionUpdated(object sender, EventArgs e)
        {
            this.PlayersView = CollectionViewSource.GetDefaultView(this.ServerRCON.Players);
            this.PlayersView.Filter = new Predicate<object>(PlayerFilter);

            SortPlayers();
            this.PlayersView?.Refresh();
        }

        private void PlayerList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.RCONParameters.PlayerListWidth = this.playerListColumn.Width.Value;
            if (this.RCONParameters.Server != null)
            {
                this.RCONParameters.Server.Profile.RCONPlayerListWidth = this.RCONParameters.PlayerListWidth;
            }
        }

        private void Runtime_StatusUpdate(object sender, EventArgs e)
        {
            this.RCONParameters.ProfileName = this.RCONParameters?.Server?.Runtime?.ProfileSnapshot.ProfileName ?? "<unknown>";
            this.RCONParameters.MaxPlayers = this.RCONParameters?.Server?.Runtime?.MaxPlayers ?? 0;
        }
        #endregion

        #region Methods
        public static void CloseAllWindows()
        {
            var windows = RCONWindows.Values.ToList();
            foreach (var window in windows)
            {
                if (window.IsLoaded)
                    window.Close();
            }
            windows.Clear();
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

        private void SetPlayerListWidth(double value)
        {
            if (value < this.playerListColumn.MinWidth)
                this.playerListColumn.Width = new GridLength(this.playerListColumn.MinWidth, GridUnitType.Pixel);
            else
                this.playerListColumn.Width = new GridLength(value, GridUnitType.Pixel);
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
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.TribeName), ListSortDirection.Ascending));
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.PlayerName), ListSortDirection.Ascending));
                    break;

                case PlayerSortType.LastUpdated:
                    this.PlayersView.SortDescriptions.Add(new SortDescription(nameof(PlayerInfo.LastUpdated), ListSortDirection.Descending));
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
                    yield return new RCONOutput_Broadcast(command.args);
                }
            }

            if (!found)
            {
                yield return new RCONOutput_Command($"> {command.rawCommand}");
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
                        yield return new RCONOutput_NoResponse();
                    }
                    else if (trimmed.EndsWith("joined the game."))
                    {
                        yield return new RCONOutput_PlayerJoined(trimmed);
                    }
                    else if (trimmed.EndsWith("left the game."))
                    {
                        yield return new RCONOutput_PlayerLeft(trimmed);
                    }
                    else
                    {
                        yield return new RCONOutput_CommandOutput(trimmed);
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
                        yield return new RCONOutput_NoResponse();
                    }
                    else
                    {
                        yield return new RCONOutput_CommandOutput(trimmed);
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

                p.Inlines.Add(new RCONOutput_Comment(trimmed));
            }

            AddBlockContent(p);
        }

        private void RenderConnectionStateChange(DependencyPropertyChangedEventArgs e)
        {
            var oldStatus = (ConsoleStatus)e.OldValue;
            var newStatus = (ConsoleStatus)e.NewValue;

            if(oldStatus != newStatus)
            {
                var p = new Paragraph();
                if (newStatus == ConsoleStatus.Connected)
                {
                    p.Inlines.Add(new RCONOutput_ConnectionChanged(true));
                }
                else
                {
                    p.Inlines.Add(new RCONOutput_ConnectionChanged(false));
                }

                AddBlockContent(p);
            }
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

            if (!(command.suppressCommand && command.suppressOutput))
            {
                if (p.Inlines.Count > 0)
                {
                    AddBlockContent(p);
                }
            }
        }
        #endregion

        #region Filtering
        private void FilterPlayerList_Click(object sender, RoutedEventArgs e)
        {
            this.PlayersView?.Refresh();
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

            result = player.PlayerNameFilterString != null && player.PlayerNameFilterString.Contains(filterString) ||
                    player.TribeNameFilterString != null && player.TribeNameFilterString.Contains(filterString) ||
                    player.CharacterNameFilterString != null && player.CharacterNameFilterString.Contains(filterString);

            return result;
        }
        #endregion
    }
}
