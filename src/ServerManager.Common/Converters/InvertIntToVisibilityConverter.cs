using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class InvertIntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var scaledValue = System.Convert.ToInt32(value);
            return scaledValue == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("InvertIntToVisibilityConverter is a OneWay converter.");
        }
    }
}
