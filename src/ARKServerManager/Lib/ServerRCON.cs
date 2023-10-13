using ArkData;
using NLog;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Extensions;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib.ViewModel.RCON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib
{
    public class ServerRcon : DependencyObject, IAsyncDisposable
    {
        private const int STEAM_UPDATE_INTERVAL = 60;
        private const int PLAYER_LIST_INTERVAL = 5000;
        private const int GET_CHAT_INTERVAL = 1000;
        private const string NoResponseMatch = "Server received, But no response!!";
        public const string NoResponseOutput = "NO_RESPONSE";

        public const string RCON_COMMAND_LISTPLAYERS = "listplayers";
        public const string RCON_COMMAND_GETCHAT = "getchat";
        public const string RCON_COMMAND_SERVERCHAT = "serverchat";
        public const string RCON_COMMAND_WILDDINOWIPE = "DestroyWildDinos";
        public const string RCON_COMMAND_KICKPLAYER = "KickPlayer";
        public const string RCON_COMMAND_BANPLAYER = "BanPlayer";
        public const string RCON_COMMAND_UNBANPLAYER = "UnbanPlayer";

        public event EventHandler PlayersCollectionUpdated;

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(ConsoleStatus), typeof(ServerRcon), new PropertyMetadata(ConsoleStatus.Disconnected));
        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register(nameof(Players), typeof(SortableObservableCollection<PlayerInfo>), typeof(ServerRcon), new PropertyMetadata(null));
        public static readonly DependencyProperty CountPlayersProperty = DependencyProperty.Register(nameof(CountPlayers), typeof(int), typeof(ServerRcon), new PropertyMetadata(0));
        public static readonly DependencyProperty CountInvalidPlayersProperty = DependencyProperty.Register(nameof(CountInvalidPlayers), typeof(int), typeof(ServerRcon), new PropertyMetadata(0));
        public static readonly DependencyProperty CountOnlinePlayersProperty = DependencyProperty.Register(nameof(CountOnlinePlayers), typeof(int), typeof(ServerRcon), new PropertyMetadata(0));

        private static readonly char[] lineSplitChars = new char[] { '\n' };
        private static readonly char[] argsSplitChars = new char[] { ' ' };

        private readonly ActionQueue _commandProcessor = new ActionQueue(TaskScheduler.Default);
        private readonly ActionQueue _outputProcessor = new ActionQueue(TaskScheduler.FromCurrentSynchronizationContext());
        private readonly List<CommandListener> _commandListeners = new List<CommandListener>();

        private readonly RCONParameters _rconParameters;
        private QueryMaster.Rcon _console;
        private int _maxCommandRetries = 3;

        private readonly ConcurrentDictionary<string, PlayerInfo> _players = new ConcurrentDictionary<string, PlayerInfo>();
        private readonly object _updatePlayerCollectionLock = new object();
        private CancellationTokenSource _cancellationTokenSource = null;

        private readonly Logger _chatLogger;
        private readonly Logger _allLogger;
        private readonly Logger _eventLogger;
        private readonly Logger _debugLogger;
        private readonly Logger _errorLogger;
        private bool _disposed = false;

        public ServerRcon(RCONParameters parameters)
        {
            _rconParameters = parameters;

            _allLogger = App.GetProfileLogger(_rconParameters.ProfileId, "RCON_All", LogLevel.Info, LogLevel.Info);
            _chatLogger = App.GetProfileLogger(_rconParameters.ProfileId, "RCON_Chat", LogLevel.Info, LogLevel.Info);
            _eventLogger = App.GetProfileLogger(_rconParameters.ProfileId, "RCON_Event", LogLevel.Info, LogLevel.Info);
            _debugLogger = App.GetProfileLogger(_rconParameters.ProfileId, "RCON_Debug", LogLevel.Trace, LogLevel.Debug);
            _errorLogger = App.GetProfileLogger(_rconParameters.ProfileId, "RCON_Error", LogLevel.Error, LogLevel.Fatal);

            this.Players = new SortableObservableCollection<PlayerInfo>();
        }

        public async ValueTask DisposeAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            await _commandProcessor.DisposeAsync();
            await _outputProcessor.DisposeAsync();

            for (int index = _commandListeners.Count - 1; index >= 0; index--)
            {
                _commandListeners[index].Dispose();
            }

            _disposed = true;
        }

        #region Properties
        public ConsoleStatus Status
        {
            get { return (ConsoleStatus)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        public SortableObservableCollection<PlayerInfo> Players
        {
            get { return (SortableObservableCollection<PlayerInfo>)GetValue(PlayersProperty); }
            set { SetValue(PlayersProperty, value); }
        }

        public int CountPlayers
        {
            get { return (int)GetValue(CountPlayersProperty); }
            set { SetValue(CountPlayersProperty, value); }
        }

        public int CountInvalidPlayers
        {
            get { return (int)GetValue(CountInvalidPlayersProperty); }
            set { SetValue(CountInvalidPlayersProperty, value); }
        }

        public int CountOnlinePlayers
        {
            get { return (int)GetValue(CountOnlinePlayersProperty); }
            set { SetValue(CountOnlinePlayersProperty, value); }
        }
        #endregion

        #region Methods
        public void Initialize()
        {
            _commandProcessor.PostAction(AutoPlayerList);
            _commandProcessor.PostAction(AutoGetChat);
            UpdatePlayersAsync().DoNotWait();
        }

        private void LogEvent(LogEventType eventType, string message)
        {
            switch (eventType)
            {
                case LogEventType.All:
                    _allLogger?.Info(message);
                    return;

                case LogEventType.Chat:
                    _chatLogger?.Info(message);
                    return;

                case LogEventType.Event:
                    _eventLogger?.Info(message);
                    return;
            }
        }

        internal void OnPlayerCollectionUpdated()
        {
            PlayersCollectionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private bool Reconnect()
        {
            if (_console != null)
            {
                _console.Dispose();
                _console = null;
            }

            var endpoint = new IPEndPoint(_rconParameters.RCONHostIP, _rconParameters.RCONPort);
            var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint);
            _console = server.GetControl(_rconParameters.RCONPassword);
            return _console != null;
        }

        public IDisposable RegisterCommandListener(Action<ConsoleCommand> callback)
        {
            var listener = new CommandListener { Callback = callback, DisposeAction = UnregisterCommandListener };
            _commandListeners.Add(listener);
            return listener;
        }

        private void UnregisterCommandListener(CommandListener listener)
        {
            _commandListeners.Remove(listener);
        }
        #endregion

        #region Process Methods
        private Task AutoPlayerList()
        {
            return _commandProcessor.PostAction(() =>
            {
                ProcessInput(new ConsoleCommand() { rawCommand = RCON_COMMAND_LISTPLAYERS, suppressCommand = true, suppressOutput = true });
                Task.Delay(PLAYER_LIST_INTERVAL).ContinueWith(t => _commandProcessor.PostAction(AutoPlayerList)).DoNotWait();
            });
        }

        private Task AutoGetChat()
        {
            return _commandProcessor.PostAction(() =>
            {
                ProcessInput(new ConsoleCommand() { rawCommand = RCON_COMMAND_GETCHAT, suppressCommand = true, suppressOutput = true });
                Task.Delay(GET_CHAT_INTERVAL).ContinueWith(t => _commandProcessor.PostAction(AutoGetChat)).DoNotWait();
            });
        }

        public Task<bool> IssueCommand(string userCommand)
        {
            return _commandProcessor.PostAction(() => ProcessInput(new ConsoleCommand() { rawCommand = userCommand }));
        }

        private bool ProcessInput(ConsoleCommand command)
        {
            try
            {
                if (!command.suppressCommand)
                {
                    LogEvent(LogEventType.All, command.rawCommand);
                }

                var args = command.rawCommand.Split(argsSplitChars, 2);
                command.command = args[0];
                if (args.Length > 1)
                {
                    command.args = args[1];
                }

                var result = SendCommand(command.rawCommand);

                if (result == null)
                {
                    _debugLogger.Debug($"SendCommand '{command.rawCommand}' do not return any results.");
                }
                else
                {
                    var lines = result.Split(lineSplitChars, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToArray();

                    if (!command.suppressOutput)
                    {
                        foreach (var line in lines)
                        {
                            LogEvent(LogEventType.All, line);
                        }
                    }

                    if (lines.Length == 1 && lines[0].StartsWith(NoResponseMatch))
                    {
                        lines[0] = NoResponseOutput;
                    }

                    command.lines = lines;
                }

                command.status = ConsoleStatus.Connected;

                _outputProcessor.PostAction(() => ProcessOutput(command));
                return true;
            }
            catch (Exception ex)
            {
                _errorLogger.Error($"Failed to send command '{command.rawCommand}'. {ex.Message}");
                command.status = ConsoleStatus.Disconnected;
                _outputProcessor.PostAction(() => ProcessOutput(command));
                return false;
            }
        }

        // This is bound to the UI thread
        private void ProcessOutput(ConsoleCommand command)
        {
            //
            // Handle results
            //
            HandleCommand(command);
            NotifyCommand(command);
        }

        // This is bound to the UI thread
        private void HandleCommand(ConsoleCommand command)
        {
            //
            // Change the connection state as appropriate
            //
            this.Status = command.status;

            //
            // Perform per-command special processing to extract data
            //
            if (command.command.Equals(RCON_COMMAND_LISTPLAYERS, StringComparison.OrdinalIgnoreCase))
            {
                //
                // Update the visible player list
                //
                command.lines = HandleListPlayersCommand(command.lines);
                command.suppressOutput = command.lines.Count() == 0;
            }

            if (command.command.Equals(RCON_COMMAND_GETCHAT, StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Extract the player name from the chat
                var lines = command.lines.Where(l => !string.IsNullOrEmpty(l) && l != NoResponseOutput);
                if (lines.IsEmpty() && command.suppressCommand)
                {
                    command.suppressOutput = true;
                }
                else
                {
                    command.suppressOutput = false;
                    command.lines = lines;
                    foreach (var line in lines)
                    {
                        LogEvent(LogEventType.Chat, line);
                        LogEvent(LogEventType.All, line);
                    }
                }
            }
            
            if (command.command.Equals(RCON_COMMAND_SERVERCHAT, StringComparison.OrdinalIgnoreCase))
            {
                LogEvent(LogEventType.Chat, command.rawCommand);
                command.suppressOutput = true;
            }

            foreach (var item in GameData.GetMessageRconInputModes())
            {
                if (command.command.Equals(item.ValueMember, StringComparison.OrdinalIgnoreCase))
                {
                    LogEvent(LogEventType.Chat, command.rawCommand);
                    command.suppressOutput = true;
                }
            }
        }

        // This is bound to the UI thread
        private List<string> HandleListPlayersCommand(IEnumerable<string> commandLines)
        {
            /*
                > listplayers
                0. Bletch, 76561197991984752 <- steam
                0. Bletch, 7171521174456260817 <- epic
            */
            var matchRegex = new Regex(@"^(?<index>\d*)\. (?<playerName>.*), (?<playerId>\d*)$");
            var output = new List<string>();
            var onlinePlayers = new List<PlayerInfo>();

            var playerLines = commandLines?.ToList() ?? new List<string>();
            if (playerLines.Count > 0)
            {
                foreach (var line in playerLines)
                {
                    var isMatch = matchRegex.IsMatch(line);
                    if (!isMatch)
                        continue;

                    var match = matchRegex.Match(line);
                    var playerName = match.Groups["playerName"].Value.Trim();
                    var playerId = match.Groups["playerId"].Value.Trim();

                    if (onlinePlayers.FirstOrDefault(p => p.PlayerId.Equals(playerId, StringComparison.OrdinalIgnoreCase)) != null)
                        // Duplicate data. Ignore it.
                        continue;

                    var newPlayer = new PlayerInfo()
                    {
                        PlayerId = playerId,
                        PlayerName = playerName,
                        IsOnline = true,
                    };
                    onlinePlayers.Add(newPlayer);

                    var playerJoined = false;
                    if (!string.IsNullOrWhiteSpace(newPlayer.PlayerName))
                    {
                        _players.AddOrUpdate(newPlayer.PlayerId,
                            (k) =>
                            {
                                playerJoined = true;
                                return newPlayer;
                            },
                            (k, v) =>
                            {
                                playerJoined = !v.IsOnline;
                                v.PlayerName = newPlayer.PlayerName;
                                v.IsOnline = newPlayer.IsOnline;
                                return v;
                            }
                        );
                    }

                    if (playerJoined)
                    {
                        var messageFormat = GlobalizedApplication.Instance.GetResourceString("Player_Join") ?? "'{0}' joined the game.";
                        var message = string.Format(messageFormat, newPlayer.PlayerName);
                        output.Add(message);
                        LogEvent(LogEventType.Event, message);
                        LogEvent(LogEventType.All, message);
                    }
                }
            }

            var droppedPlayers = _players.Values.Where(p => onlinePlayers.FirstOrDefault(np => np.PlayerId.Equals(p.PlayerId, StringComparison.OrdinalIgnoreCase)) == null);
            foreach (var droppedPlayer in droppedPlayers)
            {
                if (droppedPlayer.IsOnline)
                {
                    droppedPlayer.IsOnline = false;

                    var messageFormat = GlobalizedApplication.Instance.GetResourceString("Player_Leave") ?? "'{0}' left the game.";
                    var message = string.Format(messageFormat, droppedPlayer.PlayerName);
                    output.Add(message);
                    LogEvent(LogEventType.Event, message);
                    LogEvent(LogEventType.All, message);
                }
            }

            UpdatePlayerCollection();

            return output;
        }

        // This is bound to the UI thread
        private void NotifyCommand(ConsoleCommand command)
        {
            foreach (var listener in _commandListeners)
            {
                try
                {
                    listener.Callback(command);
                }
                catch (Exception ex)
                {
                    _errorLogger.Error("Exception in command listener: {0}\n{1}", ex.Message, ex.StackTrace);
                }
            }
        }

        private string SendCommand(string command)
        {
            const int RETRY_DELAY = 100;

            Exception lastException = null;
            int retries = 0;

            while (retries < _maxCommandRetries)
            {
                if (_console != null)
                {
                    try
                    {
                        return _console.SendCommand(command);
                    }
                    catch (Exception ex)
                    {
                        // we will simply retry
                        lastException = ex;
                    }

                    Task.Delay(RETRY_DELAY).Wait();
                }

                try
                {
                    Reconnect();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                retries++;
            }

            _maxCommandRetries = 10;
            _errorLogger.Error($"Failed to connect to RCON at {_rconParameters.RCONHostIP}:{_rconParameters.RCONPort}. {lastException.Message}");
            throw new Exception($"Command failed to send after {_maxCommandRetries} attempts.  Last exception: {lastException.Message}", lastException);
        }
        #endregion

        private async Task UpdatePlayersAsync()
        {
            if (_disposed)
                return;

            _cancellationTokenSource = new CancellationTokenSource();

            await UpdatePlayerDetailsAsync(_cancellationTokenSource.Token);
            var cancelled = _cancellationTokenSource.IsCancellationRequested;

            if (!cancelled)
            {
                await TaskUtils.RunOnUIThreadAsync(() => UpdatePlayerCollection());
            }

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;

            if (!cancelled)
            {
                await Task.Delay(PLAYER_LIST_INTERVAL)
                    .ContinueWith(t => UpdatePlayersAsync());
            }
        }

        private async Task UpdatePlayerDetailsAsync(CancellationToken token)
        {
            if (_disposed)
                return;

            if (string.IsNullOrWhiteSpace(_rconParameters.InstallDirectory))
                return;

            DataContainer dataContainer = null;

            try
            {
                var savedPath = ServerProfile.GetProfileSavePath(_rconParameters.InstallDirectory, _rconParameters.AltSaveDirectoryName, _rconParameters.PGM_Enabled, _rconParameters.PGM_Name);

                // load the player data from the files.
                dataContainer = await DataContainer.CreateAsync(savedPath, savedPath);
            }
            catch (Exception ex)
            {
                _errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: CreateAsync. {ex.Message}\r\n{ex.StackTrace}");
                return;
            }

            if (token.IsCancellationRequested)
                return;

            await Task.Run(() =>
            {
                // update the player data with the latest update value from the players collection
                foreach (var playerData in dataContainer.Players)
                {
                    var id = playerData.PlayerId;
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    _players.TryGetValue(id, out PlayerInfo player);
                    player?.UpdatePlatformData(playerData);
                }
            }, token);

            try
            {
                // load the player data from steam
                await dataContainer.LoadSteamAsync(SteamUtils.SteamWebApiKey, STEAM_UPDATE_INTERVAL);
            }
            catch (Exception ex)
            {
                _errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: LoadSteamAsync. {ex.Message}\r\n{ex.StackTrace}");
            }

            if (token.IsCancellationRequested)
                return;

            foreach (var playerData in dataContainer.Players)
            {
                if (token.IsCancellationRequested)
                    return;

                await Task.Run(async () =>
                {
                    var id = playerData.PlayerId;
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        id = Path.GetFileNameWithoutExtension(playerData.Filename);
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            _debugLogger.Debug($"{nameof(UpdatePlayerDetailsAsync)} - Error: corrupted profile.\r\n{playerData.Filename}.");
                        }
                        else
                        {
                            var invalidPlayer = new PlayerInfo()
                            {
                                PlayerId = id,
                                PlayerName = "< corrupted profile >",
                                IsValid = false,
                            };

                            _players.AddOrUpdate(id, invalidPlayer, (k, v) => { v.PlayerName = "< corrupted profile >"; v.IsValid = false; return v; });
                        }
                    }
                    else
                    {
                        var validPlayer = new PlayerInfo()
                        {
                            PlayerId = id,
                            PlayerName = playerData.PlayerName,
                            IsValid = true,
                        };

                        _players.AddOrUpdate(id, validPlayer, (k, v) => { v.PlayerName = playerData.PlayerName; v.IsValid = true; return v; });
                    }

                    if (_players.TryGetValue(id, out PlayerInfo player) && player != null)
                    {
                        player.UpdateData(playerData);

                        await TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            player.IsAdmin = _rconParameters?.Server?.Profile?.ServerFilesAdmins?.Any(u => u.PlayerId.Equals(player.PlayerId, StringComparison.OrdinalIgnoreCase)) ?? false;
                            player.IsWhitelisted = _rconParameters?.Server?.Profile?.ServerFilesWhitelisted?.Any(u => u.PlayerId.Equals(player.PlayerId, StringComparison.OrdinalIgnoreCase)) ?? false;
                        });
                    }
                }, token);
            }

            if (token.IsCancellationRequested)
                return;

            // remove any players that do not have a player file.
            var droppedPlayers = _players.Values.Where(p => dataContainer.Players.FirstOrDefault(pd => pd.PlayerId.Equals(p.PlayerId, StringComparison.OrdinalIgnoreCase)) == null).ToArray();
            foreach (var droppedPlayer in droppedPlayers)
            {
                _players.TryRemove(droppedPlayer.PlayerId, out PlayerInfo player);
            }
        }

        private void UpdatePlayerCollection()
        {
            lock (_updatePlayerCollectionLock)
            {
                this.Players = new SortableObservableCollection<PlayerInfo>(_players.Values);
                this.CountPlayers = this.Players.Count;
                this.CountInvalidPlayers = this.Players.Count(p => !p.IsValid);
                this.CountOnlinePlayers = this.Players.Count(p => p.IsOnline);

                OnPlayerCollectionUpdated();
            }
        }
    }
}
