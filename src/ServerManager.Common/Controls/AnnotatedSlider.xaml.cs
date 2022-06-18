using System.Windows;
using System.Windows.Controls;

namespace ServerManagerTool.Common.Controls
{
    /// <summary>
    /// Interaction logic for AnnotatedSlider.xaml
    /// </summary>
    public partial class AnnotatedSlider : UserControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(AnnotatedSlider));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(float), typeof(AnnotatedSlider), new FrameworkPropertyMetadata(default(float), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register(nameof(Suffix), typeof(string), typeof(AnnotatedSlider));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty LargeChangeProperty = DependencyProperty.Register(nameof(LargeChange), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty SmallChangeProperty = DependencyProperty.Register(nameof(SmallChange), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty TickFrequencyProperty = DependencyProperty.Register(nameof(TickFrequency), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty LabelRelativeWidthProperty = DependencyProperty.Register(nameof(LabelRelativeWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("4*"));
        public static readonly DependencyProperty LabelRelativeMinWidthProperty = DependencyProperty.Register(nameof(LabelRelativeMinWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("0"));
        public static readonly DependencyProperty SliderRelativeWidthProperty = DependencyProperty.Register(nameof(SliderRelativeWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("8*"));
        public static readonly DependencyProperty SliderRelativeMinWidthProperty = DependencyProperty.Register(nameof(SliderRelativeMinWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("0"));
        public static readonly DependencyProperty ValueRelativeWidthProperty = DependencyProperty.Register(nameof(ValueRelativeWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("2*"));
        public static readonly DependencyProperty ValueRelativeMinWidthProperty = DependencyProperty.Register(nameof(ValueRelativeMinWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("50"));
        public static readonly DependencyProperty ValueRelativeMinHeightProperty = DependencyProperty.Register(nameof(ValueRelativeMinHeight), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("25"));
        public static readonly DependencyProperty SuffixRelativeWidthProperty = DependencyProperty.Register(nameof(SuffixRelativeWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("1*"));
        public static readonly DependencyProperty SuffixRelativeMinWidthProperty = DependencyProperty.Register(nameof(SuffixRelativeMinWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("0"));

        public AnnotatedSlider()
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
        
        public float Value
        {
            get { return (float)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string Suffix
        {
            get { return (string)GetValue(SuffixProperty); }
            set { SetValue(SuffixProperty, value); }
        }

        public float Minimum
        {
            get { return (float)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public float Maximum
        {
            get { return (float)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public float LargeChange
        {
            get { return (float)GetValue(LargeChangeProperty); }
            set { SetValue(LargeChangeProperty, value); }
        }

        public float SmallChange
        {
            get { return (float)GetValue(SmallChangeProperty); }
            set { SetValue(SmallChangeProperty, value); }
        }

        public float TickFrequency
        {
            get { return (float)GetValue(TickFrequencyProperty); }
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
            if(Slider.IsFocused)
            {
                unchecked
                {
                    Value = (float)e.NewValue;
                }
            }
        }

        public new bool Focus()
        {
            return Slider.Focus();
        }
    }
}
