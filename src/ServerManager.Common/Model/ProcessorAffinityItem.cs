using System.Numerics;
using System.Windows;

namespace ServerManagerTool.Common.Model
{
    public class ProcessorAffinityItem : DependencyObject
    {
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(bool), typeof(ProcessorAffinityItem), new PropertyMetadata(false));
        public bool Selected
        {
            get { return (bool)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(ProcessorAffinityItem), new PropertyMetadata(string.Empty));
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty AffinityValueProperty = DependencyProperty.Register(nameof(AffinityValue), typeof(BigInteger), typeof(ProcessorAffinityItem), new PropertyMetadata(BigInteger.Zero));
        public BigInteger AffinityValue
        {
            get { return (BigInteger)GetValue(AffinityValueProperty); }
            set { SetValue(AffinityValueProperty, value); }
        }
    }
}
