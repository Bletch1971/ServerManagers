using ConanData;
using NLog;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Interfaces;
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

        public const string RCON_COMMAND_BROADCAST = "broadcast";
        public const string RCON_COMMAND_LISTPLAYERS = "#managerplayerlist#";

        public event EventHandler PlayersCollectionUpdated;

        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register(nameof(Players), typeof(SortableObservableCollection<PlayerInfo>), typeof(ServerRcon), new PropertyMetadata(null));
        public static readonly DependencyProperty CountPlayersProperty = DependencyProperty.Register(nameof(CountPlayers), typeof(int), typeof(ServerRcon), new PropertyMetadata(0));
        public static readonly DependencyProperty CountInvalidPlayersProperty = DependencyProperty.Register(nameof(CountInvalidPlayers), typeof(int), typeof(ServerRcon), new PropertyMetadata(0));
        public static readonly DependencyProperty CountOnlinePlayersProperty = DependencyProperty.Register(nameof(CountOnlinePlayers), typeof(int), typeof(ServerRcon), new PropertyMetadata(0));

        private static readonly ConcurrentDictionary<string, bool> locks = new ConcurrentDictionary<string, bool>();
        private static readonly char[] lineSplitChars = new char[] { '\n' };
        private static readonly char[] argsSplitChars = new char[] { ' ' };

        private readonly ActionQueue _commandProcessor = new ActionQueue(TaskScheduler.Default);
        private readonly ActionQueue _outputProcessor = new ActionQueue(TaskScheduler.FromCurrentSynchronizationContext());
        private readonly List<CommandListener> _commandListeners = new List<CommandListener>();

        private RconParameters _rconParameters;
        private QueryMaster.Rcon _console;
        private int maxCommandRetries = 3;

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
            this._rconParameters = parameters;
            this.Players = new SortableObservableCollection<PlayerInfo>();

            _allLogger = App.GetProfileLogger(this._rconParameters.ProfileId, "Rcon_All", LogLevel.Info, LogLevel.Info);
            _chatLogger = App.GetProfileLogger(this._rconParameters.ProfileId, "Rcon_Chat", LogLevel.Info, LogLevel.Info);
            _eventLogger = App.GetProfileLogger(this._rconParameters.ProfileId, "Rcon_Event", LogLevel.Info, LogLevel.Info);
            _debugLogger = App.GetProfileLogger(this._rconParameters.ProfileId, "Rcon_Debug", LogLevel.Trace, LogLevel.Debug);
            _errorLogger = App.GetProfileLogger(this._rconParameters.ProfileId, "Rcon_Error", LogLevel.Error, LogLevel.Fatal);

            UpdatePlayersAsync().DoNotWait();
        }

        public async Task DisposeAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
            await this._commandProcessor.DisposeAsync();
            await this._outputProcessor.DisposeAsync();

            for (int index = this._commandListeners.Count - 1; index >= 0; index--)
            {
                this._commandListeners[index].Dispose();
            }

            _disposed = true;
        }

        #region Properties
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
            if (this._console != null)
            {
                this._console.Dispose();
                this._console = null;
            }

            var endpoint = new IPEndPoint(this._rconParameters.RconHostIP, this._rconParameters.RconPort);
            var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint, sendTimeOut: 10000, receiveTimeOut: 10000);
            this._console = server.GetControl(this._rconParameters.RconPassword);
            return this._console != null;
        }

        public IDisposable RegisterCommandListener(Action<ConsoleCommand> callback)
        {
            var listener = new CommandListener { Callback = callback, DisposeAction = UnregisterCommandListener };
            this._commandListeners.Add(listener);
            return listener;
        }

        private void UnregisterCommandListener(CommandListener listener)
        {
            this._commandListeners.Remove(listener);
        }
        #endregion

        #region Process Methods
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

                this._outputProcessor.PostAction(() => ProcessOutput(command));
                return true;
            }
            catch (Exception ex)
            {
                _errorLogger?.Error($"Failed to send command '{command.rawCommand}'. {ex.Message}");
                command.status = ConsoleStatus.Disconnected;
                this._outputProcessor.PostAction(() => ProcessOutput(command));
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

        public Task<bool> IssueCommand(string userCommand)
        {
            return this._commandProcessor.PostAction(() => ProcessInput(new ConsoleCommand() { rawCommand = userCommand }));
        }

        // This is bound to the UI thread
        private void HandleCommand(ConsoleCommand command)
        {
            //
            // Perform per-command special processing to extract data
            //
            if (command?.command?.Equals(RCON_COMMAND_BROADCAST, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                LogEvent(LogEventType.Chat, command.rawCommand);
                command.suppressOutput = true;
            }

            if (command?.command?.Equals(RCON_COMMAND_LISTPLAYERS, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                command.suppressCommand = true;
                command.suppressOutput = false;
            }
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

            while (retries < maxCommandRetries)
            {
                if (this._console != null)
                {
                    try
                    {
                        return this._console.SendCommand(command);
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

            this.maxCommandRetries = 10;
            _errorLogger?.Error($"Failed to connect to Rcon at {this._rconParameters.RconHostIP}:{this._rconParameters.RconPort} with {this._rconParameters.RconPassword}. {lastException.Message}");
            throw new Exception($"Command failed to send after {maxCommandRetries} attempts.  Last exception: {lastException.Message}", lastException);
        }
        #endregion

        private async Task UpdatePlayersAsync()
        {
            if (this._disposed)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            var output = await UpdatePlayerDetailsAsync(_cancellationTokenSource.Token);
            var cancelled = _cancellationTokenSource.IsCancellationRequested;

            if (!cancelled)
            {
                await TaskUtils.RunOnUIThreadAsync(() =>
                {
                    UpdatePlayerCollection();

                    var command = new ConsoleCommand() { rawCommand = RCON_COMMAND_LISTPLAYERS, command = RCON_COMMAND_LISTPLAYERS, lines = output };
                    ProcessOutput(command);
                });
            }

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;

            if (!cancelled)
            {
                await Task.Delay(PLAYER_LIST_INTERVAL).ContinueWith(t => UpdatePlayersAsync());
            }
        }

        private async Task<List<string>> UpdatePlayerDetailsAsync(CancellationToken token)
        {
            if (this._disposed)
                return new List<string>();

            if (string.IsNullOrWhiteSpace(_rconParameters.GameFile) || !File.Exists(_rconParameters.GameFile))
                return new List<string>();

            var savedPath = ServerProfile.GetProfileSavePath(_rconParameters.InstallDirectory);
            DataContainer dataContainer = null;

            try
            {
                // load the player data from the files.
                dataContainer = await DataContainer.CreateAsync(_rconParameters.GameFile);
            }
            catch (Exception ex)
            {
                _errorLogger?.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: CreateAsync. {ex.Message}\r\n{ex.StackTrace}");
                return new List<string>();
            }

            if (token.IsCancellationRequested)
                return new List<string>();

            await Task.Run(() =>
            {
                // update the player data with the latest update value from the players collection
                foreach (var playerData in dataContainer.Players)
                {
                    var id = playerData.PlayerId;
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    this._players.TryGetValue(id, out PlayerInfo player);
                    player?.UpdatePlatformData(playerData);
                }
            }, token);

            if (token.IsCancellationRequested)
                return new List<string>();

            var totalPlayers = dataContainer.Players.Count;
            var output = new List<string>();

            foreach (var playerData in dataContainer.Players)
            {
                token.ThrowIfCancellationRequested();
                await Task.Run(async () =>
                {
                    var id = playerData.PlayerId;
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        var validPlayer = new PlayerInfo()
                        {
                            PlayerId = id,
                            PlayerName = playerData.PlayerName,
                            IsValid = true,
                        };

                        this._players.AddOrUpdate(id, validPlayer, (k, v) => { v.PlayerName = playerData.PlayerName; v.IsValid = true; return v; });
                    }
                    else
                    {
                        _debugLogger?.Debug($"{nameof(UpdatePlayerDetailsAsync)} - Error: corrupted profile.\r\n{playerData.CharacterId}.");
                    }

                    if (this._players.TryGetValue(id, out PlayerInfo player) && player != null)
                    {
                        if (player.IsOnline != playerData.Online)
                        {
                            if (playerData.Online)
                            {
                                var messageFormat = GlobalizedApplication.Instance.GetResourceString("Player_Join") ?? "Player '{0}' joined the game.";
                                var message = string.Format(messageFormat, playerData.CharacterName);
                                output.Add(message);
                                LogEvent(LogEventType.Event, message);
                                LogEvent(LogEventType.All, message);
                            }
                            else
                            {
                                var messageFormat = GlobalizedApplication.Instance.GetResourceString("Player_Leave") ?? "Player '{0}' left the game.";
                                var message = string.Format(messageFormat, playerData.CharacterName);
                                output.Add(message);
                                LogEvent(LogEventType.Event, message);
                                LogEvent(LogEventType.All, message);
                            }
                        }

                        player.UpdateData(playerData);

                        await TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            player.IsWhitelisted = _rconParameters?.Server?.Profile?.ServerFilesWhitelisted?.Any(u => u.PlayerId.Equals(player.PlayerId, StringComparison.OrdinalIgnoreCase)) ?? false;
                        });
                    }
                }, token);
            }

            if (token.IsCancellationRequested)
                return new List<string>();

            // remove any players that do not have a player record.
            var droppedPlayers = this._players.Values.Where(p => dataContainer.Players.FirstOrDefault(pd => pd.PlayerId.Equals(p.PlayerId, StringComparison.OrdinalIgnoreCase)) == null);
            foreach (var droppedPlayer in droppedPlayers)
            {
                _players.TryRemove(droppedPlayer.PlayerId, out PlayerInfo player);
            }

            return output;
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
