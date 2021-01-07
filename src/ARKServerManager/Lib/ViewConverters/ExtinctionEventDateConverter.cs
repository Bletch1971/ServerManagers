using ServerManagerTool.Utils;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Lib.ViewModel
{
    public class ExtinctionEventDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double scaledValue = System.Convert.ToInt32(value);

            if (scaledValue <= 0)
                return string.Empty;

            var displayValue = ModUtils.UnixTimeStampToDateTime(scaledValue);
            return displayValue.ToString(CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
