using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace ServerManagerTool.Common.Converters
{
    public class FloatRangeValueConverter : MarkupExtension, IValueConverter
    {
        public const string DEFAULT_CULTURE_CODE = "en-US";
        protected float MinValue { get; set; }
        protected float MaxValue { get; set; }

        public FloatRangeValueConverter()
        {
            MinValue = float.MinValue;
            MaxValue = float.MaxValue;
        }

        public FloatRangeValueConverter(float minValue)
        {
            MinValue = minValue;
            MaxValue = float.MaxValue;
        }

        public FloatRangeValueConverter(float minValue, float maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var scaledValue = System.Convert.ToSingle(value);

            var sliderValue = scaledValue;
            sliderValue = Math.Max(MinValue, sliderValue);
            sliderValue = Math.Min(MaxValue, sliderValue);
            return sliderValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || value.ToString() == string.Empty)
                return default;

            if (!float.TryParse(value.ToString(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out float sliderValue))
                return default;

            sliderValue = Math.Max(MinValue, sliderValue);
            sliderValue = Math.Min(MaxValue, sliderValue);

            var scaledValue = sliderValue;
            return scaledValue;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
