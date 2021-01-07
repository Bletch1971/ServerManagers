using ConanData;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib.ViewModel;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for PlayerProfileWindow.xaml
    /// </summary>
    public partial class PlayerProfileWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public PlayerProfileWindow(PlayerInfo player, String serverFolder)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.Player = player;
            this.ServerFolder = serverFolder;
            this.DataContext = this;
        }

        public PlayerInfo Player
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

        public Boolean IsGuildOwner => PlayerData != null && GuildData != null && GuildData.OwnerId == PlayerData.CharacterId;

        public String GuildOwner => GuildData != null && GuildData.Owner != null ? $"{GuildData.Owner.CharacterName}" : null;

        public String WindowTitle => String.Format(_globalizer.GetResourceString("Profile_WindowTitle_Player"), Player.PlayerName);

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
