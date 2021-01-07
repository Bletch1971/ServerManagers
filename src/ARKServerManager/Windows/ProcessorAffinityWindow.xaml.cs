using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using System.Numerics;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for ProcessorAffinityWindow.xaml
    /// </summary>
    public partial class ProcessorAffinityWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty ProcessorAffinityListProperty = DependencyProperty.Register(nameof(ProcessorAffinityList), typeof(ProcessorAffinityList), typeof(ProcessorAffinityWindow), new PropertyMetadata(null));
        public ProcessorAffinityList ProcessorAffinityList
        {
            get { return (ProcessorAffinityList)GetValue(ProcessorAffinityListProperty); }
            set { SetValue(ProcessorAffinityListProperty, value); }
        }

        public ProcessorAffinityWindow(string profileName, BigInteger affinityValue)
        {
            AffinityValue = affinityValue;

            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.Title = string.Format(_globalizer.GetResourceString("ProcessorAffinity_ProfileTitle"), profileName);
            this.ProcessorAffinityList = new ProcessorAffinityList(affinityValue);

            this.DataContext = this;
        }

        public BigInteger AffinityValue
        {
            get;
            set;
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            AffinityValue = this.ProcessorAffinityList.AffinityValue;

            DialogResult = true;
            Close();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ProcessorAffinityList)
            {
                item.Selected = true;
            }
        }

        private void UnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ProcessorAffinityList)
            {
                item.Selected = false;
            }
        }
    }
}
