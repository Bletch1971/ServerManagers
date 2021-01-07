using System;
using System.Windows.Data;

namespace ServerManagerTool.Lib.ViewModel
{
    public class OfficialDifficultyValueConverter : IValueConverter
    {
        public const double DINO_LEVELS = 30.0f;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var scaledValue = System.Convert.ToDouble(value);
            scaledValue = Math.Max(1.0, scaledValue);

            var sliderValue = scaledValue * DINO_LEVELS;
            sliderValue = Math.Max(DINO_LEVELS, sliderValue);

            return (int)sliderValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var sliderValue = System.Convert.ToDouble(value);
            sliderValue = Math.Max(DINO_LEVELS, sliderValue);

            var scaledValue = sliderValue / DINO_LEVELS;
            scaledValue = Math.Max(1.0, scaledValue);

            return scaledValue;
        }
    }
}
