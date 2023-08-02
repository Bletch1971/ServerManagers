using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class InvertStringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("InvertStringNullOrEmptyToVisibilityConverter is a OneWay converter.");
        }
    }
}
