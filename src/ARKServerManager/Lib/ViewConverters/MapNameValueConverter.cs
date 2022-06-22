using ServerManagerTool.Utils;
using System;
using System.Globalization;
using System.Windows.Data;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib.ViewModel
{
    public class MapNameValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var valueString = value as string;
                if (valueString == null)
                    return string.Empty;

                var name = GlobalizedApplication.Instance.GetResourceString($"Map_{valueString}");
                if (!string.IsNullOrWhiteSpace(name))
                    return name;

                var mapName = ModUtils.GetMapName(valueString);

                // check if the name is stored in the globalization file
                name = GlobalizedApplication.Instance.GetResourceString($"Map_{mapName}");
                if (!string.IsNullOrWhiteSpace(name))
                    return name;

                if (!string.IsNullOrWhiteSpace(mapName))
                    return mapName;

                return valueString;
            }
            catch
            {
                return value ?? string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
