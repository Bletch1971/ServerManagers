using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Lib.ViewModel
{
    public class DinoNameValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var strVal = value as string;
            return strVal.Substring(0, strVal.IndexOf('_'));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
