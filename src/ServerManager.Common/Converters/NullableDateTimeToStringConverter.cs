using ServerManagerTool.Common.Model;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ServerManagerTool.Common.Converters
{
    public class NullableDateTimeToStringConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is NullableValue<DateTime> && ((NullableValue<DateTime>)value).Value != DateTime.MinValue)
                return ((NullableValue<DateTime>)value).Value.ToString("yyyy.MM.dd HH:mm:ss");

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || value.ToString() == string.Empty)
                return (new NullableValue<DateTime>());

            if (!DateTime.TryParse(value.ToString(), out DateTime datetime))
                return (new NullableValue<DateTime>());

            return (new NullableValue<DateTime>(datetime));
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}