using ArkData;
using NLog;
using ServerManagerTool.Common.Extensions;
using ServerManagerTool.Common.Interfaces;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib.ViewModel;
using ServerManagerTool.Lib.ViewModel.RCON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib
{
    public class ServerRCON : DependencyObject, IAsyncDisposable
    {
        public event EventHandler PlayersCollectionUpdated;

        private const int STEAM_UPDATE_INTERVAL = 60;
        private const int PLAYER_LIST_INTERVAL = 5000;
        private const int GET_CHAT_INTERVAL = 1000;
        private const string NoResponseMatch = "Server received, But no response!!";
        public const string NoResponseOutput = "NO_RESPONSE";

        public const string RCON_COMMAND_BROADCAST = "broadcast";
        public const string RCON_COMMAND_LISTPLAYERS = "listplayers";
        public const string RCON_COMMAND_GETCHAT = "getchat";
        public const string RCON_COMMAND_SERVERCHAT = "serverchat";
        public const string RCON_COMMAND_WILDDINOWIPE = "DestroyWildDinos";

        [TypeConverter(typeof(EnumDescriptionTypeConverter))]
        public enum ConsoleStatus
        {
            Disconnected,
            Connected,
        };

        public class ConsoleCommand
        {
            public ConsoleStatus status;
            public string rawCommand;

            public string command;
            public string args;

            public bool suppressCommand;
            public bool suppressOutput;
            public IEnumerable<string> lines = new string[0];
        };

        private class CommandListener : IDisposable
        {
            public Action<ConsoleCommand> Callback { get; set; }
            public Action<CommandListener> DisposeAction { get; set; }

            public void Dispose()
            {
                DisposeAction(this);
            }
        }

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(ConsoleStatus), typeof(ServerRCON), new PropertyMetadata(ConsoleStatus.Disconnected));
        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register(nameof(Players), typeof(SortableObservableCollection<PlayerInfo>), typeof(ServerRCON), new PropertyMetadata(null));
        public static readonly DependencyProperty CountPlayersProperty = DependencyProperty.Register(nameof(CountPlayers), typeof(int), typeof(ServerRCON), new PropertyMetadata(0));
        public static readonly DependencyProperty CountInvalidPlayersProperty = DependencyProperty.Register(nameof(CountInvalidPlayers), typeof(int), typeof(ServerRCON), new PropertyMetadata(0));
        public static readonly DependencyProperty CountOnlinePlayersProperty = DependencyProperty.Register(nameof(CountOnlinePlayers), typeof(int), typeof(ServerRCON), new PropertyMetadata(0));

        private static readonly char[] lineSplitChars = new char[] { '\n' };
        private static readonly char[] argsSplitChars = new char[] { ' ' };
        private readonly ActionQueue commandProcessor = new ActionQueue(TaskScheduler.Default);
        private readonly ActionQueue outputProcessor = new ActionQueue(TaskScheduler.FromCurrentSynchronizationContext());
        private readonly List<CommandListener> commandListeners = new List<CommandListener>();
        private readonly RCONParameters rconParams;
        private QueryMaster.Rcon console;
        private int maxCommandRetries = 3;

        private readonly ConcurrentDictionary<string, PlayerInfo> players = new ConcurrentDictionary<string, PlayerInfo>();
        private readonly object updatePlayerCollectionLock = new object();
        private CancellationTokenSource cancellationTokenSource = null;

        private readonly Logger _chatLogger;
        private readonly Logger _allLogger;
        private readonly Logger _eventLogger;
        private readonly Logger _debugLogger;
        private readonly Logger _errorLogger;
        private bool _disposed = false;

        public ServerRCON(RCONParameters parameters)
        {
            this.rconParams = parameters;
            this.Players = new SortableObservableCollection<PlayerInfo>();

            _allLogger = App.GetProfileLogger(this.rconParams.ProfileId, "RCON_All", LogLevel.Info, LogLevel.Info);
            _chatLogger = App.GetProfileLogger(this.rconParams.ProfileId, "RCON_Chat", LogLevel.Info, LogLevel.Info);
            _eventLogger = App.GetProfileLogger(this.rconParams.ProfileId, "RCON_Event", LogLevel.Info, LogLevel.Info);
            _debugLogger = App.GetProfileLogger(this.rconParams.ProfileId, "RCON_Debug", LogLevel.Trace, LogLevel.Debug);
            _errorLogger = App.GetProfileLogger(this.rconParams.ProfileId, "RCON_Error", LogLevel.Error, LogLevel.Fatal);
        }

        public async Task DisposeAsync()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
            await this.commandProcessor.DisposeAsync();
            await this.outputProcessor.DisposeAsync();

            for (int index = this.commandListeners.Count - 1; index >= 0; index--)
            {
                this.commandListeners[index].Dispose();
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
            commandProcessor.PostAction(AutoPlayerList);
            commandProcessor.PostAction(AutoGetChat);
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
            if (this.console != null)
            {
                this.console.Dispose();
                this.console = null;
            }

            var endpoint = new IPEndPoint(this.rconParams.RCONHostIP, this.rconParams.RCONPort);
            var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint);
            this.console = server.GetControl(this.rconParams.AdminPassword);
            return this.console != null;
        }

        public IDisposable RegisterCommandListener(Action<ConsoleCommand> callback)
        {
            var listener = new CommandListener { Callback = callback, DisposeAction = UnregisterCommandListener };
            this.commandListeners.Add(listener);
            return listener;
        }

        private void UnregisterCommandListener(CommandListener listener)
        {
            this.commandListeners.Remove(listener);
        }
        #endregion

        #region Process Methods
        private Task AutoPlayerList()
        {
            return commandProcessor.PostAction(() =>
            {
                ProcessInput(new ConsoleCommand() { rawCommand = RCON_COMMAND_LISTPLAYERS, suppressCommand = true, suppressOutput = true });
                Task.Delay(PLAYER_LIST_INTERVAL).ContinueWith(t => commandProcessor.PostAction(AutoPlayerList)).DoNotWait();
            });
        }

        private Task AutoGetChat()
        {
            return commandProcessor.PostAction(() =>
            {
                ProcessInput(new ConsoleCommand() { rawCommand = RCON_COMMAND_GETCHAT, suppressCommand = true, suppressOutput = true });
                Task.Delay(GET_CHAT_INTERVAL).ContinueWith(t => commandProcessor.PostAction(AutoGetChat)).DoNotWait();
            });
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

                this.outputProcessor.PostAction(() => ProcessOutput(command));
                return true;
            }
            catch (Exception ex)
            {
                _errorLogger.Error($"Failed to send command '{command.rawCommand}'. {ex.Message}");
                command.status = ConsoleStatus.Disconnected;
                this.outputProcessor.PostAction(() => ProcessOutput(command));
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
            return this.commandProcessor.PostAction(() => ProcessInput(new ConsoleCommand() { rawCommand = userCommand }));
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
                command.suppressOutput = false;
                command.lines = HandleListPlayersCommand(command.lines);
            }
            else if (command.command.Equals(RCON_COMMAND_GETCHAT, StringComparison.OrdinalIgnoreCase))
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
            else if (command.command.Equals(RCON_COMMAND_BROADCAST, StringComparison.OrdinalIgnoreCase))
            {
                LogEvent(LogEventType.Chat, command.rawCommand);
                command.suppressOutput = true;
            }
            else if (command.command.Equals(RCON_COMMAND_SERVERCHAT, StringComparison.OrdinalIgnoreCase))
            {
                LogEvent(LogEventType.Chat, command.rawCommand);
                command.suppressOutput = true;
            }
        }

        // This is bound to the UI thread
        private List<string> HandleListPlayersCommand(IEnumerable<string> commandLines)
        {
            var output = new List<string>();

            if (commandLines != null)
            {
                var onlinePlayers = new List<PlayerInfo>();
                foreach (var line in commandLines)
                {
                    var elements = line.Split(',');
                    if (elements.Length != 2)
                        // Invalid data. Ignore it.
                        continue;

                    var id = elements[1]?.Trim();
                    if (id.Any(c => !char.IsDigit(c)))
                        // Invalid data. Ignore it.
                        continue;

                    if (onlinePlayers.FirstOrDefault(p => p.PlayerId.Equals(id, StringComparison.OrdinalIgnoreCase)) != null)
                        // Duplicate data. Ignore it.
                        continue;

                    var newPlayer = new PlayerInfo()
                    {
                        PlayerId = id,
                        PlayerName = elements[0].Substring(elements[0].IndexOf('.') + 1).Trim(),
                        IsOnline = true,
                    };
                    onlinePlayers.Add(newPlayer);

                    var playerJoined = false;
                    this.players.AddOrUpdate(newPlayer.PlayerId, (k) => { playerJoined = true; return newPlayer; }, (k, v) => { playerJoined = !v.IsOnline; v.IsOnline = true; return v; });

                    if (playerJoined)
                    {
                        var messageFormat = GlobalizedApplication.Instance.GetResourceString("Player_Join") ?? "Player '{0}' joined the game.";
                        var message = string.Format(messageFormat, newPlayer.PlayerName);
                        output.Add(message);
                        LogEvent(LogEventType.Event, message);
                        LogEvent(LogEventType.All, message);
                    }
                }

                var droppedPlayers = this.players.Values.Where(p => onlinePlayers.FirstOrDefault(np => np.PlayerId.Equals(p.PlayerId, StringComparison.OrdinalIgnoreCase)) == null);
                foreach (var droppedPlayer in droppedPlayers)
                {
                    if (droppedPlayer.IsOnline)
                    {
                        droppedPlayer.IsOnline = false;

                        var messageFormat = GlobalizedApplication.Instance.GetResourceString("Player_Leave") ?? "Player '{0}' left the game.";
                        var message = string.Format(messageFormat, droppedPlayer.PlayerName);
                        output.Add(message);
                        LogEvent(LogEventType.Event, message);
                        LogEvent(LogEventType.All, message);
                    }
                }

                UpdatePlayerCollection();
            }

            return output;
        }

        // This is bound to the UI thread
        private void NotifyCommand(ConsoleCommand command)
        {
            foreach (var listener in commandListeners)
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

            while (retries < maxCommandRetries)
            {
                if (this.console != null)
                {
                    try
                    {
                        return this.console.SendCommand(command);
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
            _errorLogger.Error($"Failed to connect to RCON at {this.rconParams.RCONHostIP}:{this.rconParams.RCONPort} with {this.rconParams.AdminPassword}. {lastException.Message}");
            throw new Exception($"Command failed to send after {maxCommandRetries} attempts.  Last exception: {lastException.Message}", lastException);
        }
        #endregion

        private async Task UpdatePlayersAsync()
        {
            if (this._disposed)
                return;

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            await UpdatePlayerDetailsAsync(cancellationTokenSource.Token)
                .ContinueWith(async t1 =>
                {
                    await TaskUtils.RunOnUIThreadAsync(() =>
                    {
                        UpdatePlayerCollection();
                    });
                }, TaskContinuationOptions.NotOnCanceled)
                .ContinueWith(t2 =>
                {
                    var cancelled = cancellationTokenSource.IsCancellationRequested;
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;

                    if (!cancelled)
                        Task.Delay(PLAYER_LIST_INTERVAL).ContinueWith(t3 => UpdatePlayersAsync());
                });
        }

        private async Task UpdatePlayerDetailsAsync(CancellationToken token)
        {
            if (!string.IsNullOrWhiteSpace(rconParams.InstallDirectory))
            {
                var savedPath = ServerProfile.GetProfileSavePath(rconParams.InstallDirectory, rconParams.AltSaveDirectoryName, rconParams.PGM_Enabled, rconParams.PGM_Name);
                DataContainer dataContainer = null;
                DateTime lastSteamUpdateUtc = DateTime.MinValue;

                try
                {
                    // load the player data from the files.
                    dataContainer = await DataContainer.CreateAsync(savedPath, savedPath);
                }
                catch (Exception ex)
                {
                    _errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: CreateAsync. {ex.Message}\r\n{ex.StackTrace}");
                    return;
                }

                token.ThrowIfCancellationRequested();
                await Task.Run(() =>
                {
                    // update the player data with the latest update value from the players collection
                    foreach (var playerData in dataContainer.Players)
                    {
                        var id = playerData.PlayerId;
                        if (string.IsNullOrWhiteSpace(id))
                            continue;

                        this.players.TryGetValue(id, out PlayerInfo player);
                        player?.UpdatePlatformData(playerData);
                    }
                }, token);

                try
                {
                    // load the player data from steam
                    lastSteamUpdateUtc = await dataContainer.LoadSteamAsync(SteamUtils.SteamWebApiKey, STEAM_UPDATE_INTERVAL);
                }
                catch (Exception ex)
                {
                    _errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: LoadSteamAsync. {ex.Message}\r\n{ex.StackTrace}");
                }

                token.ThrowIfCancellationRequested();

                var totalPlayers = dataContainer.Players.Count;
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
                                PlayerId = playerData.PlayerId,
                                PlayerName = playerData.PlayerName,
                                IsValid = true,
                            };

                            this.players.AddOrUpdate(id, validPlayer, (k, v) => { v.PlayerName = playerData.PlayerName; v.IsValid = true; return v; });
                        }
                        else
                        {
                            id = Path.GetFileNameWithoutExtension(playerData.Filename);
                            if (string.IsNullOrWhiteSpace(id))
                            {
                                var invalidPlayer = new PlayerInfo()
                                {
                                    PlayerId = id,
                                    PlayerName = "< corrupted profile >",
                                    IsValid = false,
                                };

                                this.players.AddOrUpdate(id, invalidPlayer, (k, v) => { v.PlayerName = "< corrupted profile >"; v.IsValid = false; return v; });
                            }
                            else
                            {
                                _debugLogger.Debug($"{nameof(UpdatePlayerDetailsAsync)} - Error: corrupted profile.\r\n{playerData.Filename}.");
                            }
                        }

                        if (this.players.TryGetValue(id, out PlayerInfo player) && player != null)
                        {
                            player.UpdateData(playerData);

                            await TaskUtils.RunOnUIThreadAsync(() =>
                            {
                                player.IsAdmin = rconParams?.Server?.Profile?.ServerFilesAdmins?.Any(u => u.PlayerId.Equals(player.PlayerId, StringComparison.OrdinalIgnoreCase)) ?? false;
                                player.IsWhitelisted = rconParams?.Server?.Profile?.ServerFilesWhitelisted?.Any(u => u.PlayerId.Equals(player.PlayerId, StringComparison.OrdinalIgnoreCase)) ?? false;
                            });
                        }
                    }, token);
                }

                token.ThrowIfCancellationRequested();

                // remove any players that do not have a player file.
                var droppedPlayers = this.players.Values.Where(p => dataContainer.Players.FirstOrDefault(pd => pd.PlayerId.Equals(p.PlayerId, StringComparison.OrdinalIgnoreCase)) == null);
                foreach (var droppedPlayer in droppedPlayers)
                {
                    players.TryRemove(droppedPlayer.PlayerId, out PlayerInfo player);
                }
            }
        }

        private void UpdatePlayerCollection()
        {
            lock (updatePlayerCollectionLock)
            {
                this.Players = new SortableObservableCollection<PlayerInfo>(players.Values);
                this.CountPlayers = this.Players.Count;
                this.CountInvalidPlayers = this.Players.Count(p => !p.IsValid);
                this.CountOnlinePlayers = this.Players.Count(p => p.IsOnline);

                OnPlayerCollectionUpdated();
            }
        }
    }
}
