using System.Windows;
using System.Windows.Controls;

namespace ServerManagerTool.Common.Controls
{
    /// <summary>
    /// Interaction logic for AnnotatedSlider.xaml
    /// </summary>
    public partial class AnnotatedIntSlider : UserControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(AnnotatedIntSlider));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(int), typeof(AnnotatedIntSlider), new FrameworkPropertyMetadata(default(int), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register(nameof(Suffix), typeof(string), typeof(AnnotatedIntSlider));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(AnnotatedIntSlider));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(AnnotatedIntSlider));
        public static readonly DependencyProperty LargeChangeProperty = DependencyProperty.Register(nameof(LargeChange), typeof(int), typeof(AnnotatedIntSlider));
        public static readonly DependencyProperty SmallChangeProperty = DependencyProperty.Register(nameof(SmallChange), typeof(int), typeof(AnnotatedIntSlider));
        public static readonly DependencyProperty TickFrequencyProperty = DependencyProperty.Register(nameof(TickFrequency), typeof(int), typeof(AnnotatedIntSlider));
        public static readonly DependencyProperty LabelRelativeWidthProperty = DependencyProperty.Register(nameof(LabelRelativeWidth), typeof(string), typeof(AnnotatedIntSlider), new PropertyMetadata("4*"));
        public static readonly DependencyProperty LabelRelativeMinWidthProperty = DependencyProperty.Register(nameof(LabelRelativeMinWidth), typeof(string), typeof(AnnotatedIntSlider), new PropertyMetadata("0"));
        public static readonly DependencyProperty SliderRelativeWidthProperty = DependencyProperty.Register(nameof(SliderRelativeWidth), typeof(string), typeof(AnnotatedIntSlider), new PropertyMetadata("8*"));
        public static readonly DependencyProperty SliderRelativeMinWidthProperty = DependencyProperty.Register(nameof(SliderRelativeMinWidth), typeof(string), typeof(AnnotatedIntSlider), new PropertyMetadata("0"));
        public static readonly DependencyProperty ValueRelativeWidthProperty = DependencyProperty.Register(nameof(ValueRelativeWidth), typeof(string), typeof(AnnotatedIntSlider), new PropertyMetadata("2*"));
        public static readonly DependencyProperty ValueRelativeMinWidthProperty = DependencyProperty.Register(nameof(ValueRelativeMinWidth), typeof(string), typeof(AnnotatedIntSlider), new PropertyMetadata("50"));
        public static readonly DependencyProperty ValueRelativeMinHeightProperty = DependencyProperty.Register(nameof(ValueRelativeMinHeight), typeof(string), typeof(AnnotatedIntSlider), new PropertyMetadata("25"));
        public static readonly DependencyProperty SuffixRelativeWidthProperty = DependencyProperty.Register(nameof(SuffixRelativeWidth), typeof(string), typeof(AnnotatedIntSlider), new PropertyMetadata("1*"));
        public static readonly DependencyProperty SuffixRelativeMinWidthProperty = DependencyProperty.Register(nameof(SuffixRelativeMinWidth), typeof(string), typeof(AnnotatedIntSlider), new PropertyMetadata("0"));

        public AnnotatedIntSlider()
        {
            InitializeComponent();

            this.Focusable = true;

            (this.Content as FrameworkElement).DataContext = this;
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }
        
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string Suffix
        {
            get { return (string)GetValue(SuffixProperty); }
            set { SetValue(SuffixProperty, value); }
        }

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public int LargeChange
        {
            get { return (int)GetValue(LargeChangeProperty); }
            set { SetValue(LargeChangeProperty, value); }
        }

        public int SmallChange
        {
            get { return (int)GetValue(SmallChangeProperty); }
            set { SetValue(SmallChangeProperty, value); }
        }

        public int TickFrequency
        {
            get { return (int)GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        public string LabelRelativeWidth
        {
            get { return (string)GetValue(LabelRelativeWidthProperty); }
            set { SetValue(LabelRelativeWidthProperty, value); }
        }

        public string LabelRelativeMinWidth
        {
            get { return (string)GetValue(LabelRelativeMinWidthProperty); }
            set { SetValue(LabelRelativeMinWidthProperty, value); }
        }

        public string SliderRelativeWidth
        {
            get { return (string)GetValue(SliderRelativeWidthProperty); }
            set { SetValue(SliderRelativeWidthProperty, value); }
        }

        public string SliderRelativeMinWidth
        {
            get { return (string)GetValue(SliderRelativeMinWidthProperty); }
            set { SetValue(SliderRelativeMinWidthProperty, value); }
        }

        public string ValueRelativeWidth
        {
            get { return (string)GetValue(ValueRelativeWidthProperty); }
            set { SetValue(ValueRelativeWidthProperty, value); }
        }

        public string ValueRelativeMinWidth
        {
            get { return (string)GetValue(ValueRelativeMinWidthProperty); }
            set { SetValue(ValueRelativeMinWidthProperty, value); }
        }

        public string ValueRelativeMinHeight
        {
            get { return (string)GetValue(ValueRelativeMinHeightProperty); }
            set { SetValue(ValueRelativeMinHeightProperty, value); }
        }

        public string SuffixRelativeWidth
        {
            get { return (string)GetValue(SuffixRelativeWidthProperty); }
            set { SetValue(SuffixRelativeWidthProperty, value); }
        }

        public string SuffixRelativeMinWidth
        {
            get { return (string)GetValue(SuffixRelativeMinWidthProperty); }
            set { SetValue(SuffixRelativeMinWidthProperty, value); }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Slider.IsFocused)
            {
                unchecked
                {
                    Value = (int)e.NewValue;
                }
            }
        }

        public new bool Focus()
        {
            return Slider.Focus();
        }
    }
}
