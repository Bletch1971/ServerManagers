using System.Windows;

namespace ServerManagerTool.Lib
{
    public class PlayerListParameters : DependencyObject
    {
        public static readonly DependencyProperty ProfileNameProperty = DependencyProperty.Register(nameof(ProfileName), typeof(string), typeof(PlayerListParameters), new PropertyMetadata(string.Empty));

        public string ProfileName
        {
            get { return (string)GetValue(ProfileNameProperty); }
            set { SetValue(ProfileNameProperty, value); }
        }

        public string ProfileId { get; set; }

        public string InstallDirectory { get; set; }

        public string AltSaveDirectoryName { get; set; }

        public bool PGM_Enabled { get; set; }

        public string PGM_Name { get; set; }

        public Server Server { get; set; }

        public string ServerMap { get; set; }

        public Rect WindowExtents { get; set; }

        public string WindowTitle { get; set; }
    }
}
