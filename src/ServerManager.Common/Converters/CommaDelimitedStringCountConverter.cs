using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class CommaDelimitedStringCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null)
            {
                return "0";
            }

            var strValue = value as string;
            if(string.IsNullOrWhiteSpace(strValue))
            {
                return "0";
            }

            return strValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("CommaDelimitedStringCountConverter can only be used OneWay.");
        }
    }
}
