using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ServerManagerTool.Common.Converters
{
    public class BooleanEqualsConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var convertedValue = System.Convert.ToBoolean(value);
            var parameterValue = System.Convert.ToBoolean(parameter);
            return convertedValue.Equals(parameterValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var convertedValue = System.Convert.ToBoolean(value);
            var parameterValue = System.Convert.ToBoolean(parameter);
            return convertedValue ? parameterValue : Binding.DoNothing;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
