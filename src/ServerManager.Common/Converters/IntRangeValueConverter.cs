using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ServerManagerTool.Common.Converters
{
    public class IntRangeValueConverter : MarkupExtension, IValueConverter
    {
        protected int MinValue { get; set; }
        protected int MaxValue { get; set; }

        public IntRangeValueConverter()
        {
            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
        }

        public IntRangeValueConverter(int minValue)
        {
            MinValue = minValue;
            MaxValue = int.MaxValue;
        }

        public IntRangeValueConverter(int minValue, int maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double scaledValue = System.Convert.ToInt32(value);

            var sliderValue = scaledValue;
            sliderValue = Math.Max(MinValue, sliderValue);
            sliderValue = Math.Min(MaxValue, sliderValue);
            return sliderValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || value.ToString() == string.Empty)
                return default;

            if (!int.TryParse(value.ToString(), out int sliderValue))
                return default;

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
