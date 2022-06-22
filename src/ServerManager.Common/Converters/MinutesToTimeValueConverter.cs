using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class MinutesToTimeValueConverter : IValueConverter
    {
        public const int MAX_VALUE_HOURS = 24 * 365;
        public const int MAX_VALUE_MINUTES = 59;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Value is seconds since midnight.
            var totalMinutes = (int)value;
            var hours = Math.Min(Math.Max(totalMinutes / 60, 0), MAX_VALUE_HOURS);
            var minutes = Math.Min(Math.Max(totalMinutes % 60, 0), MAX_VALUE_MINUTES);
            return String.Format("{0:00}:{1:00}", hours, minutes);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var strTime = (string)value;
            var split = strTime.Split(':');
            if(split.Length != 2)
            {
                return 0;
            }

            int.TryParse(split[0], out int hours);
            int.TryParse(split[1], out int minutes);

            return hours * 60 + minutes;
        }
    }
}
