using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class NullableHasValueConverter : IValueConverter
    {
        public object convertValue = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            convertValue = value;
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value)
            {
                if (targetType == typeof(bool) || targetType == typeof(bool?))
                    return default(bool);
                else if (targetType == typeof(int) || targetType == typeof(int?))
                    return default(int);
                else if (targetType == typeof(float) || targetType == typeof(float?))
                    return default(float);

                return string.Empty;
            }

            return null;
        }
    }
}
