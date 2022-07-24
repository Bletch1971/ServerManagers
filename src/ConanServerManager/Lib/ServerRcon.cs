using ConanData;
using NLog;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib.ViewModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib
{
    public class ServerRcon : DependencyObject, IAsyncDisposable
    {
        private const int PLAYER_LIST_INTERVAL = 5000;
        private const string NoResponseMatch = "Server received, But no response!!";
        public const string NoResponseOutput = "NO_RESPONSE";

        public const string RCON_COMMAND_LISTPLAYERS = "listplayers";

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

        private RconParameters _rconParameters;
        private QueryMaster.Rcon _console;
        private int _maxCommandRetries = 3;

        private readonly ConcurrentDictionary<string, PlayerInfo> _players = new ConcurrentDictionary<string, PlayerInfo>();
        private readonly object _updatePlayerCollectionLock = new object();
        private CancellationTokenSource _cancellationTokenSource = null;

        private Logger _chatLogger;
        private Logger _allLogger;
        private Logger _eventLogger;
        private Logger _debugLogger;
        private Logger _errorLogger;
        private bool _disposed = false;

        public ServerRcon(RconParameters parameters)
        {
            _rconParameters = parameters;

            _allLogger = App.GetProfileLogger(_rconParameters.ProfileId, "Rcon_All", LogLevel.Info, LogLevel.Info);
            _chatLogger = App.GetProfileLogger(_rconParameters.ProfileId, "Rcon_Chat", LogLevel.Info, LogLevel.Info);
            _eventLogger = App.GetProfileLogger(_rconParameters.ProfileId, "Rcon_Event", LogLevel.Info, LogLevel.Info);
            _debugLogger = App.GetProfileLogger(_rconParameters.ProfileId, "Rcon_Debug", LogLevel.Trace, LogLevel.Debug);
            _errorLogger = App.GetProfileLogger(_rconParameters.ProfileId, "Rcon_Error", LogLevel.Error, LogLevel.Fatal);

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

            var endpoint = new IPEndPoint(_rconParameters.RconHostIP, _rconParameters.RconPort);
            var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint, sendTimeOut: 10000, receiveTimeOut: 10000);
            _console = server.GetControl(_rconParameters.RconPassword);
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
                    _debugLogger?.Debug($"SendCommand '{command.rawCommand}' do not return any results.");
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
                _errorLogger?.Error($"Failed to send command '{command.rawCommand}'. {ex.Message}");
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
            // Perform per-command special processing to extract data
            //
            if (command?.command?.Equals(RCON_COMMAND_LISTPLAYERS, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                command.lines = HandleListPlayersCommand(command.lines);
                command.suppressOutput = command.lines.Count() == 0;
            }

            foreach (var item in GameData.GetMessageRconInputModes())
            {
                if (command?.command?.Equals(item.ValueMember, StringComparison.OrdinalIgnoreCase) ?? false)
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
                Idx | Char name        | Player name  | User ID          | Platform ID       | Platform Name
                0 | Alora Truthrider | Bletch#80041 | CE9DBB451DD05733 | 76561197991984752 | Steam
            */
            var output = new List<string>();
            var onlinePlayers = new List<PlayerInfo>();

            var playerLines = commandLines?.ToList() ?? new List<string>();
            if (playerLines.Count > 1)
            {
                // remove the first line, as it is a header row
                playerLines.RemoveAt(0);

                foreach (var line in playerLines)
                {
                    var elements = line.Split('|');
                    if (elements.Length != 6)
                        // Invalid data. Ignore it.
                        continue;

                    var id = elements[3].Trim();

                    if (onlinePlayers.FirstOrDefault(p => p.PlayerId.Equals(id, StringComparison.OrdinalIgnoreCase)) != null)
                        // Duplicate data. Ignore it.
                        continue;

                    var newPlayer = new PlayerInfo()
                    {
                        PlayerId = id,
                        PlayerName = elements[2].Trim(),
                        CharacterName = elements[1].Trim(),
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
                        var message = string.Format(messageFormat, newPlayer.CharacterName);
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
                    var message = string.Format(messageFormat, droppedPlayer.CharacterName);
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
                    _errorLogger?.Error("Exception in command listener: {0}\n{1}", ex.Message, ex.StackTrace);
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
            _errorLogger?.Error($"Failed to connect to Rcon at {_rconParameters.RconHostIP}:{_rconParameters.RconPort}. {lastException.Message}");
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

            if (string.IsNullOrWhiteSpace(_rconParameters.GameFile) || !File.Exists(_rconParameters.GameFile))
                return;

            DataContainer dataContainer = null;

            try
            {
                // load the player data from the files.
                dataContainer = await DataContainer.CreateAsync(_rconParameters.GameFile);
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
                        _debugLogger?.Debug($"{nameof(UpdatePlayerDetailsAsync)} - Error: corrupted profile.\r\n{playerData.CharacterId}.");
                    }
                    else
                    {
                        var validPlayer = new PlayerInfo()
                        {
                            PlayerId = id,
                            PlayerName = playerData.PlayerName,
                            CharacterName = playerData.CharacterName,
                            IsValid = true,
                        };

                        _players.AddOrUpdate(id, validPlayer, (k, v) => { v.PlayerName = playerData.PlayerName; v.CharacterName = playerData.CharacterName;  v.IsValid = true; return v; });
                    }

                    if (_players.TryGetValue(id, out PlayerInfo player) && player != null)
                    {
                        player.UpdateData(playerData);

                        await TaskUtils.RunOnUIThreadAsync(() =>
                        {
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
