using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class FlagsEnumToBooleanConverter : IValueConverter
    {
        private int _targetValue;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mask = System.Convert.ToInt32(parameter);
            _targetValue = System.Convert.ToInt32(value);

            return (mask & _targetValue) != 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _targetValue ^= System.Convert.ToInt32(parameter);
            return Enum.Parse(targetType, _targetValue.ToString());
        }
    }
}
