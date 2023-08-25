using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ServerManagerTool.Common.Converters
{
    public class Int64RangeValueConverter : MarkupExtension, IValueConverter
    {
        public const string DEFAULT_CULTURE_CODE = "en-US";
        protected Int64 MinValue { get; set; }
        protected Int64 MaxValue { get; set; }

        public Int64RangeValueConverter()
        {
            MinValue = Int64.MinValue;
            MaxValue = Int64.MaxValue;
        }

        public Int64RangeValueConverter(Int64 minValue)
        {
            MinValue = minValue;
            MaxValue = Int64.MaxValue;
        }

        public Int64RangeValueConverter(Int64 minValue, Int64 maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var scaledValue = System.Convert.ToInt64(value);

            var sliderValue = scaledValue;
            sliderValue = Math.Max(MinValue, sliderValue);
            sliderValue = Math.Min(MaxValue, sliderValue);
            return sliderValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || value.ToString() == string.Empty)
                return default;

            if (!Int64.TryParse(value.ToString(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out Int64 sliderValue))
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
