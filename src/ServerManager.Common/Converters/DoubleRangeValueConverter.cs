using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ServerManagerTool.Common.Converters
{
    public class DoubleRangeValueConverter : MarkupExtension, IValueConverter
    {
        protected double MinValue { get; set; }
        protected double MaxValue { get; set; }

        public DoubleRangeValueConverter()
        {
            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
        }

        public DoubleRangeValueConverter(double minValue)
        {
            MinValue = minValue;
            MaxValue = int.MaxValue;
        }

        public DoubleRangeValueConverter(double minValue, double maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var scaledValue = System.Convert.ToDouble(value);

            var sliderValue = scaledValue;
            sliderValue = Math.Max(MinValue, sliderValue);
            sliderValue = Math.Min(MaxValue, sliderValue);
            return sliderValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sliderValue = System.Convert.ToDouble(value);
            sliderValue = Math.Max(MinValue, sliderValue);
            sliderValue = Math.Min(MaxValue, sliderValue);

            var scaledValue = sliderValue;
            return scaledValue;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
