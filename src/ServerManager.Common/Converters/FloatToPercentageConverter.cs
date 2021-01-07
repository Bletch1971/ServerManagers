using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class FloatToPercentageConverter : IValueConverter
    {
        public const int MIN_VALUE = 0;
        public const int MAX_VALUE = 100;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var scaledValue = System.Convert.ToSingle(value);

            var sliderValue = scaledValue * 100;
            sliderValue = Math.Max(MIN_VALUE, sliderValue);
            //sliderValue = Math.Min(MAX_VALUE, sliderValue);
            return $"{sliderValue}%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() == typeof(string))
            {
                var stringValue = (string)value;
                if (stringValue.EndsWith("%"))
                {
                    stringValue = stringValue.Replace("%", "");
                    value = stringValue;
                }
            }

            var sliderValue = System.Convert.ToSingle(value);
            sliderValue = Math.Max(MIN_VALUE, sliderValue);
            //sliderValue = Math.Min(MAX_VALUE, sliderValue);

            var scaledValue = sliderValue / 100;
            return scaledValue;
        }
    }
}
