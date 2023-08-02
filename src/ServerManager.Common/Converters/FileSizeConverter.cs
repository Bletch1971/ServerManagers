using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ServerManagerTool.Common.Converters
{
    public class FileSizeConverter : MarkupExtension, IValueConverter
    {
        private const decimal DIVISOR = 1024M;

        // Load all suffixes in an array  
        private static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var counter = 0;
            var number = System.Convert.ToDecimal(value);

            while (number / DIVISOR >= 1)
            {
                number /= DIVISOR;
                counter++;
            }

            return string.Format("{0:n2} {1}", number, suffixes[counter]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("FileSizeConverter can only be used OneWay.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}