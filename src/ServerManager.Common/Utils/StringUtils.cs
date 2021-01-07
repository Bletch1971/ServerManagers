using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using System;
using System.Globalization;
using System.Reflection;

namespace ServerManagerTool.Common.Utils
{
    public static class StringUtils
    {
        public const string DEFAULT_CULTURE_CODE = "en-US";

        public static string GetPropertyValue(object value, PropertyInfo property)
        {
            string convertedVal;

            if (property.PropertyType == typeof(float))
                convertedVal = ((float)value).ToString("0.000000####", CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            else if (property.PropertyType == typeof(string))
                convertedVal = $"\"{value}\"";
            else
                convertedVal = Convert.ToString(value, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));

            return convertedVal;
        }

        public static string GetPropertyValue(object value, PropertyInfo property, BaseIniFileEntryAttribute attribute)
        {
            string convertedVal;

            if (property.PropertyType == typeof(int) || property.PropertyType == typeof(NullableValue<int>))
                convertedVal = Convert.ToString(value, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            else if (property.PropertyType == typeof(float) || property.PropertyType == typeof(NullableValue<float>))
                convertedVal = ((float)value).ToString("0.000000####", CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            else if (property.PropertyType == typeof(bool))
            {
                var boolValue = (bool)value;
                if (attribute.InvertBoolean)
                    boolValue = !boolValue;
                if (attribute.WriteBooleanAsInteger)
                    convertedVal = boolValue ? "1" : "0";
                else
                    convertedVal = boolValue.ToString(CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            }
            else
                convertedVal = Convert.ToString(value, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));

            return convertedVal;
        }

        public static void SetPropertyValue(string value, object obj, PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                bool boolValue;
                bool.TryParse(value, out boolValue);
                property.SetValue(obj, boolValue);
            }
            else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
            {
                int intValue;
                int.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out intValue);
                property.SetValue(obj, intValue);
            }
            else if (property.PropertyType == typeof(float) || property.PropertyType == typeof(float?))
            {
                var tempValue = value.Replace("f", "");

                float floatValue;
                float.TryParse(tempValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out floatValue);
                property.SetValue(obj, floatValue);
            }
            else if (property.PropertyType == typeof(NullableValue<int>))
            {
                int intValue;
                int.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out intValue);
                var field = property.GetValue(obj) as NullableValue<int>;
                property.SetValue(obj, field.SetValue(true, intValue));
            }
            else if (property.PropertyType == typeof(NullableValue<float>))
            {
                var tempValue = value.Replace("f", "");

                float floatValue;
                float.TryParse(tempValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out floatValue);
                var field = property.GetValue(obj) as NullableValue<float>;
                property.SetValue(obj, field.SetValue(true, floatValue));
            }
            else if (property.PropertyType.IsSubclassOf(typeof(AggregateIniValue)))
            {
                var field = property.GetValue(obj) as AggregateIniValue;
                field?.InitializeFromINIValue(value);
            }
            else
            {
                var convertedValue = Convert.ChangeType(value, property.PropertyType, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
                if (convertedValue is string)
                    convertedValue = (convertedValue as string).Trim('"');
                property.SetValue(obj, convertedValue);
            }
        }

        public static bool SetPropertyValue(string value, object obj, PropertyInfo property, BaseIniFileEntryAttribute attribute)
        {
            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                bool boolValue;
                if (attribute.WriteBooleanAsInteger)
                    boolValue = value.Equals("1", StringComparison.OrdinalIgnoreCase);
                else
                    bool.TryParse(value, out boolValue);
                if (attribute.InvertBoolean)
                    boolValue = !boolValue;
                property.SetValue(obj, boolValue);
                return true;
            }
            if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
            {
                int intValue;
                int.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out intValue);
                property.SetValue(obj, intValue);
                return true;
            }
            if (property.PropertyType == typeof(float) || property.PropertyType == typeof(float?))
            {
                var tempValue = value.Replace("f", "");

                float floatValue;
                float.TryParse(tempValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out floatValue);
                property.SetValue(obj, floatValue);
                return true;
            }
            if (property.PropertyType == typeof(NullableValue<int>))
            {
                int intValue;
                int.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out intValue);
                var field = property.GetValue(obj) as NullableValue<int>;
                property.SetValue(obj, field.SetValue(true, intValue));
                return true;
            }
            if (property.PropertyType == typeof(NullableValue<float>))
            {
                var tempValue = value.Replace("f", "");

                float floatValue;
                float.TryParse(tempValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out floatValue);
                var field = property.GetValue(obj) as NullableValue<float>;
                property.SetValue(obj, field.SetValue(true, floatValue));
                return true;
            }
            if (property.PropertyType.IsSubclassOf(typeof(AggregateIniValue)))
            {
                var field = property.GetValue(obj) as AggregateIniValue;
                field?.InitializeFromINIValue(value);
                return true;
            }

            return false;
        }
    }
}
