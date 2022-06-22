using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class EnumFlagsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            var flagsValue = (int)value;
            var targetFlagValue = (int)Enum.Parse(value.GetType(), parameter.ToString());
            return (flagsValue & targetFlagValue) == targetFlagValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert flags value back");
        }
    }
}
