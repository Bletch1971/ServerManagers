using ServerManagerTool.Common.Utils;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class UnixTimeToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var unixTimestamp = System.Convert.ToInt32(value);
            return DateTimeUtils.UnixTimeStampToDateTime(unixTimestamp).ToString(culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0;
        }
    }
}
