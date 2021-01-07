using System.Windows;

namespace ServerManagerTool.Lib
{
    public class PlayerListParameters : DependencyObject
    {
        public static readonly DependencyProperty ProfileNameProperty = DependencyProperty.Register(nameof(ProfileName), typeof(string), typeof(PlayerListParameters), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty MaxPlayersProperty = DependencyProperty.Register(nameof(MaxPlayers), typeof(int), typeof(PlayerListParameters), new PropertyMetadata(0));

        public string ProfileName
        {
            get { return (string)GetValue(ProfileNameProperty); }
            set { SetValue(ProfileNameProperty, value); }
        }

        public string ProfileId { get; set; }

        public string InstallDirectory { get; set; }

        public string GameFile { get; set; }

        public Server Server { get; set; }

        public Rect WindowExtents { get; set; }

        public string WindowTitle { get; set; }

        public int MaxPlayers
        {
            get { return (int)GetValue(MaxPlayersProperty); }
            set { SetValue(MaxPlayersProperty, value); }
        }
    }
}
