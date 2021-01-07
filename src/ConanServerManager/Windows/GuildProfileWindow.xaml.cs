using ConanData;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for GuildProfileWindow.xaml
    /// </summary>
    public partial class GuildProfileWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public GuildProfileWindow(PlayerInfo player, ICollection<PlayerInfo> players, String serverFolder)
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

        public GuildData GuildData => Player?.PlayerData?.Guild;

        public String GuildOwner => GuildData != null && GuildData.Owner != null ? $"{GuildData.Owner.CharacterName}" : null;

        public ICollection<PlayerInfo> GuildPlayers
        {
            get
            {
                if (GuildData == null) return null;

                ICollection<PlayerInfo> players = new List<PlayerInfo>();
                foreach (var guildPlayer in GuildData.Players)
                {
                    var player = Players.FirstOrDefault(p => p.PlayerId.ToString() == guildPlayer.PlayerId);
                    if (player != null)
                        players.Add(player);
                }
                return players;
            }
        }

        public String WindowTitle => String.Format(_globalizer.GetResourceString("Profile_WindowTitle_Tribe"), Player.GuildName);

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
