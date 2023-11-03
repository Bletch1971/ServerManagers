using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ServerManagerTool.Common.Converters
{
    public class DateTimeToStringConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime datetime = (DateTime)value;
            if (datetime == DateTime.MinValue)
                return "";

            return datetime.ToString("yyyy.MM.dd HH:mm:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || value.ToString() == string.Empty)
                return DateTime.MinValue;

            if (!DateTime.TryParse(value.ToString(), out DateTime datetime))
                return DateTime.MinValue;

            return datetime;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}