using ConanData;
using NLog;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib.ViewModel;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ServerManagerTool.Lib
{
    public class ServerPlayers : DependencyObject
    {
        private const int PLAYER_LIST_INTERVAL = 5000;

        public event EventHandler PlayersCollectionUpdated;

        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register(nameof(Players), typeof(SortableObservableCollection<PlayerInfo>), typeof(ServerPlayers), new PropertyMetadata(null));
        public static readonly DependencyProperty CountPlayersProperty = DependencyProperty.Register(nameof(CountPlayers), typeof(int), typeof(ServerPlayers), new PropertyMetadata(0));
        public static readonly DependencyProperty CountInvalidPlayersProperty = DependencyProperty.Register(nameof(CountInvalidPlayers), typeof(int), typeof(ServerPlayers), new PropertyMetadata(0));
        public static readonly DependencyProperty CountOnlinePlayersProperty = DependencyProperty.Register(nameof(CountOnlinePlayers), typeof(int), typeof(ServerPlayers), new PropertyMetadata(0));

        private readonly ConcurrentDictionary<string, PlayerInfo> _players = new ConcurrentDictionary<string, PlayerInfo>();
        private readonly object _updatePlayerCollectionLock = new object();
        private CancellationTokenSource _cancellationTokenSource = null;
        private PlayerListParameters _playerListParameters;

        private Logger _allLogger;
        private Logger _eventLogger;
        private Logger _debugLogger;
        private Logger _errorLogger;
        private bool _disposed = false;

        public ServerPlayers(PlayerListParameters parameters)
        {
            this.Players = new SortableObservableCollection<PlayerInfo>();

            _playerListParameters = parameters;

            _allLogger = App.GetProfileLogger(_playerListParameters.ProfileId, "PlayerList_All", LogLevel.Info, LogLevel.Info);
            _eventLogger = App.GetProfileLogger(_playerListParameters.ProfileId, "PlayerList_Event", LogLevel.Info, LogLevel.Info);
            _debugLogger = App.GetProfileLogger(_playerListParameters.ProfileId, "PlayerList_Debug", LogLevel.Trace, LogLevel.Debug);
            _errorLogger = App.GetProfileLogger(_playerListParameters.ProfileId, "PlayerList_Error", LogLevel.Error, LogLevel.Fatal);

            UpdatePlayersAsync().DoNotWait();
        }

        public void Dispose()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
            _disposed = true;
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

        private void LogEvent(LogEventType eventType, string message)
        {
            switch (eventType)
            {
                case LogEventType.All:
                    _allLogger?.Info(message);
                    return;

                case LogEventType.Event:
                    _eventLogger?.Info(message);
                    return;
            }
        }

        protected void OnPlayerCollectionUpdated()
        {
            PlayersCollectionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private async Task UpdatePlayersAsync()
        {
            if (this._disposed)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            await UpdatePlayerDetailsAsync(_cancellationTokenSource.Token);
            var cancelled = _cancellationTokenSource.IsCancellationRequested;

            if (!cancelled)
            {
                await TaskUtils.RunOnUIThreadAsync(() =>
                {
                    UpdatePlayerCollection();
                });
            }

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;

            if (!cancelled)
            {
                await Task.Delay(PLAYER_LIST_INTERVAL).ContinueWith(t => UpdatePlayersAsync());
            }
        }

        private async Task UpdatePlayerDetailsAsync(CancellationToken token)
        {
            if (this._disposed)
                return;

            if (string.IsNullOrWhiteSpace(_playerListParameters.GameFile) || !File.Exists(_playerListParameters.GameFile))
                return;

            var savedPath = ServerProfile.GetProfileSavePath(_playerListParameters.InstallDirectory);
            DataContainer dataContainer = null;

            try
            {
                // load the player data from the files.
                dataContainer = await DataContainer.CreateAsync(_playerListParameters.GameFile);
            }
            catch (Exception ex)
            {
                _errorLogger?.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: CreateAsync. {ex.Message}\r\n{ex.StackTrace}");
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

                    this._players.TryGetValue(id, out PlayerInfo player);
                    player?.UpdatePlatformData(playerData);
                }
            }, token);

            if (token.IsCancellationRequested)
                return;

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
                        player.UpdateData(playerData);

                        await TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            player.IsWhitelisted = _playerListParameters?.Server?.Profile?.ServerFilesWhitelisted?.Any(u => u.PlayerId.Equals(player.PlayerId, StringComparison.OrdinalIgnoreCase)) ?? false;
                        });
                    }
                }, token);
            }

            if (token.IsCancellationRequested)
                return;

            // remove any players that do not have a player record.
            var droppedPlayers = this._players.Values.Where(p => dataContainer.Players.FirstOrDefault(pd => pd.PlayerId.Equals(p.PlayerId, StringComparison.OrdinalIgnoreCase)) == null).ToArray();
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
