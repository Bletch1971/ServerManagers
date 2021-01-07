using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Lib.ViewModel
{
    public class ExtinctionEventTimeIntervalConverter : IValueConverter
    {
        public const int MIN_VALUE = 1;
        public const int MAX_VALUE = 1000;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double scaledValue = System.Convert.ToInt32(value);

            var sliderValue = (int)TimeSpan.FromSeconds(scaledValue).TotalDays;
            sliderValue = Math.Max(MIN_VALUE, sliderValue);
            sliderValue = Math.Min(MAX_VALUE, sliderValue);
            return sliderValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sliderValue = System.Convert.ToInt32(value);
            sliderValue = Math.Max(MIN_VALUE, sliderValue);
            sliderValue = Math.Min(MAX_VALUE, sliderValue);

            var scaledValue = (int)TimeSpan.FromDays(sliderValue).TotalSeconds;
            return scaledValue;
        }
    }
}
