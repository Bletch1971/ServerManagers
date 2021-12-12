using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class FloatToStringConverter : IValueConverter
    {
        public const string DEFAULT_CULTURE_CODE = "en-US";
        public const int MIN_VALUE = 0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var floatValue = System.Convert.ToSingle(value);
            floatValue = Math.Max(MIN_VALUE, floatValue);

            return ((float)floatValue).ToString("0.000000####", CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var floatValue = System.Convert.ToSingle(value);
            floatValue = Math.Max(MIN_VALUE, floatValue);

            return floatValue;
        }
    }
}
