using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class InvertBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = false;
            if (value is bool)
            {
                flag = (bool)value;
            }
            else if (value is bool?)
            {
                bool? nullable = (bool?)value;
                flag = nullable.HasValue && nullable.Value;
            }
            return flag ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
                return (Visibility)value == Visibility.Collapsed;
            return false;
        }
    }
}
