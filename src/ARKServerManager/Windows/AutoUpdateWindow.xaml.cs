using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for AutoUpdateWindow.xaml
    /// </summary>
    public partial class AutoUpdateWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private readonly SteamCmdUpdater updater = new SteamCmdUpdater();
        private CancellationTokenSource cancelSource;

        public AutoUpdateWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.ErrorLabel.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cancelSource = new CancellationTokenSource();
            updater.UpdateSteamCmdAsync(Config.Default.DataDir, new Progress<SteamCmdUpdater.Update>(async u =>
                {
                    var message = string.IsNullOrWhiteSpace(u.StatusKey) ? string.Empty : _globalizer.GetResourceString(u.StatusKey) ?? u.StatusKey;
                    this.StatusLabel.Content = message;
                    this.CompletionProgress.Value = u.CompletionPercent;

                    if (u.FailureText != null)
                    {
                        this.ErrorLabel.Text = u.FailureText;
                        this.ErrorLabel.Visibility = Visibility.Visible;
                        await Task.Delay(10000);

                        Environment.Exit(1);
                    }

                    if (u.CompletionPercent >= 100 || u.Cancelled)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                var mainWindow = new MainWindow();
                                mainWindow.Show();
                                this.Close();
                            });
                    }
                }), cancelSource.Token);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (cancelSource != null)
                cancelSource.Cancel();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (cancelSource != null)
                cancelSource.Cancel();
        }
    }
}
