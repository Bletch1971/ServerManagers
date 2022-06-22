using System;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class SecondsToTimeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Value is seconds since midnight.
            var seconds = System.Convert.ToInt32(value);
            var hours = Math.Min(Math.Max(seconds / 3600, 0), 23);
            var minutes = Math.Min(Math.Max((seconds % 3600) / 60, 0), 59);
            return string.Format("{0:00}:{1:00}", hours, minutes);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var strTime = (string)value;
            var split = strTime.Split(':');
            if (split.Length != 2)
            {
                return 0;
            }

            int.TryParse(split[0], out int hours);
            int.TryParse(split[1], out int minutes);

            return hours * 3600 + minutes * 60;
        }
    }
}
