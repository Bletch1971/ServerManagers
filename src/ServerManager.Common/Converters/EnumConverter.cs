using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            var checkValue = value.ToString();
            var targetValue = parameter.ToString();
            return checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            var useValue = System.Convert.ToBoolean(value);
            var targetValue = parameter.ToString();
            if (useValue)
                return Enum.Parse(targetType, targetValue);

            return null;
        }
    }
}
