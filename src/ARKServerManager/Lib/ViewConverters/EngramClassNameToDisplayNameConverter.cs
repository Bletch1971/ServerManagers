using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Lib.ViewModel
{
    public class EngramClassNameToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static object Convert(object value)
        {
            try
            {
                var strVal = value as string;
                var name = GameData.FriendlyNameForClass(strVal);
                if (!string.IsNullOrWhiteSpace(name) && !name.Equals(strVal))
                    return name;

                var firstIndex = strVal.IndexOf('_') + 1;
                var length = strVal.LastIndexOf('_') - firstIndex;
                return strVal.Substring(firstIndex, length);
            }
            catch
            {
                return value;
            }
        }
    }
}
