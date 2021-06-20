using NLog;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib;
using System;
using System.ComponentModel;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for SupplyCrateOverridesWindow.xaml
    /// </summary>
    public partial class SupplyCrateOverridesWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public EventHandler<ProfileEventArgs> SavePerformed;

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private readonly ServerProfile _profile = null;

        public SupplyCrateOverridesWindow(ServerProfile profile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            _profile = profile;
            this.Title = $"{this.Title} - {_profile?.ProfileName}"; // string.Format(_globalizer.GetResourceString("SupplyCrateOverridesWindow_ProfileTitle"), _profile?.ProfileName);

            this.DataContext = this;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

        protected void OnSavePerformed()
        {
            SavePerformed?.Invoke(this, new ProfileEventArgs(_profile));
        }
    }
}
