using ServerManagerTool.Common.Utils;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for CommandLineWindow.xaml
    /// </summary>
    public partial class CommandLineWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public CommandLineWindow(string commandLine)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.DataContext = commandLine;
        }

        public TextWrapping OutputTextWrapping
        {
            get { return OutputTextBox.TextWrapping; }
            set { OutputTextBox.TextWrapping = value; }
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(this.DataContext as string);
                MessageBox.Show(_globalizer.GetResourceString("CommandLine_CopyButton_ConfirmLabel"), _globalizer.GetResourceString("CommandLine_CopyButton_ConfirmTitle"), MessageBoxButton.OK);
            }
            catch
            {
                MessageBox.Show(_globalizer.GetResourceString("CommandLine_CopyButton_ErrorLabel"), _globalizer.GetResourceString("CommandLine_CopyButton_ErrorTitle"), MessageBoxButton.OK);
            }            
        }
    }
}
