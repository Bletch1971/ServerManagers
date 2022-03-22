using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for OpenRCONWindow.xaml
    /// </summary>
    public partial class OpenRCONWindow : Window
    {
        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public string ServerIP
        {
            get { return (string)GetValue(ServerIPProperty); }
            set { SetValue(ServerIPProperty, value); }
        }

        public static readonly DependencyProperty ServerIPProperty = DependencyProperty.Register(nameof(ServerIP), typeof(string), typeof(OpenRCONWindow), new PropertyMetadata(IPAddress.Loopback.ToString()));

        public int RCONPort
        {
            get { return (int)GetValue(RCONPortProperty); }
            set { SetValue(RCONPortProperty, value); }
        }

        public static readonly DependencyProperty RCONPortProperty = DependencyProperty.Register(nameof(RCONPort), typeof(int), typeof(OpenRCONWindow), new PropertyMetadata(32330));

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(nameof(Password), typeof(string), typeof(OpenRCONWindow), new PropertyMetadata(String.Empty));

        public OpenRCONWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            LoadDefaults();
            this.DataContext = this;
        }

        public ICommand ConnectCommand => new RelayCommand<object>(
            execute: _ => {
                // set focus to the Connect button, if the Enter key is pressed, the value just entered has not yet been posted to the property.
                ConnectButton.Focus();

                var window = RCONWindow.GetRCON(new Lib.RCONParameters()
                {
                    ProfileName = $"{ServerIP} {RCONPort}",
                    ProfileId = $"{ServerIP}-{RCONPort}".Replace(".", "-"),
                    RCONHost = ServerIP,
                    RCONPort = RCONPort,
                    RCONPassword = Password,
                    InstallDirectory = String.Empty,
                    AltSaveDirectoryName = String.Empty,
                    PGM_Enabled = false,
                    PGM_Name = string.Empty,
                    WindowTitle = String.Format(_globalizer.GetResourceString("OpenRCON_WindowTitle"), ServerIP, RCONPort),
                    WindowExtents = Rect.Empty
                });

                SaveDefaults();

                window.Owner = this.Owner;
                if (this.Owner == null)
                {
                    this.Close();
                    window.ShowInTaskbar = true;
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    window.ShowDialog();
                }
                else
                {
                    window.Show();
                    this.Close();
                }
            },
            canExecute: _ => true
        );

        private void LoadDefaults()
        {
            if (!String.IsNullOrWhiteSpace(Config.Default.OpenRCON_ServerIP))
                ServerIP = Config.Default.OpenRCON_ServerIP;
            RCONPort = Config.Default.OpenRCON_RCONPort;
        }
        private void SaveDefaults()
        {
            Config.Default.OpenRCON_ServerIP = ServerIP;
            Config.Default.OpenRCON_RCONPort = RCONPort;
        }
    }
}
