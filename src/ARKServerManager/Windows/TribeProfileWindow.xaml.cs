using ArkData;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib.ViewModel.RCON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for TribeProfileWindow.xaml
    /// </summary>
    public partial class TribeProfileWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public TribeProfileWindow(PlayerInfo player, ICollection<PlayerInfo> players, String serverFolder)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.Player = player;
            this.Players = players;
            this.ServerFolder = serverFolder;
            this.DataContext = this;
        }

        public PlayerInfo Player
        {
            get;
            private set;
        }

        public ICollection<PlayerInfo> Players
        {
            get;
            private set;
        }

        public String ServerFolder
        {
            get;
            private set;
        }

        public PlayerData PlayerData => Player?.PlayerData;

        public TribeData TribeData => Player?.PlayerData?.Tribe;

        public String CreatedDate => TribeData?.FileCreated.ToString("G");

        public String TribeLink => String.IsNullOrWhiteSpace(ServerFolder) || TribeData == null ? null : $"/select, {Path.Combine(ServerFolder, $"{TribeData.Id}{Config.Default.TribeFileExtension}")}";

        public String TribeOwner => TribeData != null && TribeData.Owner != null ? $"{TribeData.Owner.CharacterName} ({TribeData.Owner.PlayerName})" : null;

        public ICollection<PlayerInfo> TribePlayers
        {
            get
            {
                if (TribeData == null) return null;

                ICollection<PlayerInfo> players = new List<PlayerInfo>();
                foreach (var tribePlayer in TribeData.Players)
                {
                    var player = Players.FirstOrDefault(p => p.PlayerId.ToString() == tribePlayer.PlayerId);
                    if (player != null)
                        players.Add(player);
                }
                return players;
            }
        }

        public String UpdatedDate => TribeData?.FileUpdated.ToString("G");

        public String WindowTitle => String.Format(_globalizer.GetResourceString("Profile_WindowTitle_Tribe"), Player.TribeName);

        public ICommand ExplorerLinkCommand
        {
            get
            {
                return new RelayCommand<String>(
                    execute: (action) =>
                    {
                        if (String.IsNullOrWhiteSpace(action)) return;
                        Process.Start("explorer.exe", action);
                    },
                    canExecute: (action) => true
                );
            }
        }
    }
}
