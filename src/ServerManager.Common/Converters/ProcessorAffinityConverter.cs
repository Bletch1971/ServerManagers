using ServerManagerTool.Common.Utils;
using System;
using System.Globalization;
using System.Numerics;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class ProcessorAffinityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!BigInteger.TryParse(value.ToString(), out BigInteger affinity))
                return "Invalid";

            if (!ProcessUtils.IsProcessorAffinityValid(affinity))
                return "Invalid";

            if (affinity == BigInteger.Zero)
                return "All";

            var result = string.Empty;
            var delimiter = string.Empty;

            var index = 0;
            while (true)
            {
                var cpuValue = (BigInteger)Math.Pow(2, index);
                if (cpuValue > affinity)
                    break;

                if ((affinity & cpuValue) == cpuValue)
                {
                    result = $"{result}{delimiter}{index}";
                    delimiter = ", ";
                }

                index++;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("ProcessorAffinityConverter can only be used OneWay.");
        }
    }
}
