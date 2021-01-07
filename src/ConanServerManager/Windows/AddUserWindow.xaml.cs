using ServerManagerTool.Common.Utils;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for AddUserWindow.xaml
    /// </summary>
    public partial class AddUserWindow : Window
    {
        public AddUserWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.DataContext = this;
        }

        public static readonly DependencyProperty UsersProperty = DependencyProperty.Register(nameof(Users), typeof(string), typeof(AddUserWindow), new PropertyMetadata(string.Empty));
        public string Users
        {
            get { return (string)GetValue(UsersProperty); }
            set { SetValue(UsersProperty, value); }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
