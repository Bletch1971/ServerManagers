using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class IsNullOrWhiteSpaceValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return true;
            }

            var strValue = value as string;
            if (string.IsNullOrWhiteSpace(strValue))
            {
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }
}
