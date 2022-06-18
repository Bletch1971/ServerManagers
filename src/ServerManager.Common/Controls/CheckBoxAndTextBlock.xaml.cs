using System.Windows;
using System.Windows.Controls;

namespace ServerManagerTool.Common.Controls
{
    /// <summary>
    /// Interaction logic for CheckBoxAndTextBlock.xaml
    /// </summary>
    public partial class CheckBoxAndTextBlock : UserControl
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(CheckBoxAndTextBlock));
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(CheckBoxAndTextBlock));

        public CheckBoxAndTextBlock()
        {
            InitializeComponent();

            this.Focusable = true;

            (this.Content as FrameworkElement).DataContext = this;
        }

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public new bool Focus()
        {
            return CheckBox.Focus();
        }

        private void Label_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.IsEnabled && this.CheckBox.IsEnabled)
            {
                this.IsChecked = !this.IsChecked;
            }
        }
    }
}
