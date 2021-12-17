using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Interfaces;
using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;

namespace ServerManagerTool.Common.Model
{
    /// <summary>
    /// An INI style value of the form AggregateName=(Key1=val1, Key2=val2...)
    /// </summary>
    public abstract class AggregateIniValue : DependencyObject
    {
        protected const char DELIMITER = ',';

        protected readonly List<PropertyInfo> Properties = new List<PropertyInfo>();

        public T Duplicate<T>() where T : AggregateIniValue, new()
        {
            GetPropertyInfos(true);

            var result = new T();
            foreach (var prop in this.Properties.Where(prop => prop.CanWrite))
            {
                prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }

        public static T FromINIValue<T>(string value) where T : AggregateIniValue, new()
        {
            var result = new T();
            result.InitializeFromINIValue(value);
            return result;
        }

        protected void GetPropertyInfos(bool allProperties = false)
        {
            if (this.Properties.Count != 0)
                return;

            if (allProperties)
                this.Properties.AddRange(this.GetType().GetProperties());
            else
                this.Properties.AddRange(this.GetType().GetProperties().Where(p => p.GetCustomAttribute(typeof(AggregateIniValueEntryAttribute)) != null));
        }

        public abstract string GetSortKey();

        public virtual void InitializeFromINIValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return;

            var kvPair = value.Split(new[] { '=' }, 2);
            value = kvPair[1].Trim('(', ')', ' ');
            var pairs = value.Split(DELIMITER);

            foreach (var pair in pairs)
            {
                kvPair = pair.Split('=');
                if (kvPair.Length != 2)
                    continue;

                var key = kvPair[0].Trim();
                var val = kvPair[1].Trim();
                var propInfo = this.Properties.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
                if (propInfo != null)
                    StringUtils.SetPropertyValue(val, this, propInfo);
                else
                {
                    propInfo = this.Properties.FirstOrDefault(f => f.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().Any(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase)));
                    if (propInfo != null)
                        StringUtils.SetPropertyValue(val, this, propInfo);
                }
            }
        }

        public abstract bool IsEquivalent(AggregateIniValue other);

        public virtual bool ShouldSave() { return true; }

        public static object SortKeySelector(AggregateIniValue arg)
        {
            return arg.GetSortKey();
        }

        public virtual string ToINIValue()
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return string.Empty;

            var result = new StringBuilder();
            result.Append("(");

            var delimiter = "";
            foreach (var prop in this.Properties)
            {
                result.Append(delimiter);

                var attr = prop.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().FirstOrDefault();
                var propName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;

                var val = prop.GetValue(this);
                var propValue = StringUtils.GetPropertyValue(val, prop, attr?.QuotedString ?? true);

                if ((attr?.ExcludeIfEmpty ?? false) && string.IsNullOrWhiteSpace(propValue))
                {
                    Debug.WriteLine($"{propName} skipped, ExcludeIfEmpty = true and value is empty");
                }
                else
                {
                    if (!(attr?.ExcludePropertyName ?? false))
                        result.Append($"{propName}=");
                    result.Append($"{propValue}");

                    delimiter = DELIMITER.ToString();
                }
            }

            result.Append(")");
            return result.ToString();
        }

        public override string ToString()
        {
            return ToINIValue();
        }

        protected virtual void FromComplexINIValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return;

            var kvValue = value.Trim(' ');

            var propertyValues = SplitCollectionValues(kvValue, DELIMITER);

            foreach (var property in this.Properties)
            {
                var attr = property.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().FirstOrDefault();
                var propertyName = string.IsNullOrWhiteSpace(attr?.Key) ? property.Name : attr.Key;

                var propertyValue = propertyValues.FirstOrDefault(p => p.StartsWith($"{propertyName}="));
                if (propertyValue == null)
                    continue;

                var kvPropertyPair = propertyValue.Split(new[] { '=' }, 2);
                var kvPropertyValue = kvPropertyPair[1].Trim(DELIMITER, ' ');

                if (attr?.ValueWithinBrackets ?? false)
                {
                    if (kvPropertyValue.StartsWith("("))
                        kvPropertyValue = kvPropertyValue.Substring(1);
                    if (kvPropertyValue.EndsWith(")"))
                        kvPropertyValue = kvPropertyValue.Substring(0, kvPropertyValue.Length - 1);
                }

                if (property.GetValue(this) is IIniValuesCollection collection)
                {
                    var values = SplitCollectionValues(kvPropertyValue, DELIMITER)
                        .Where(v => !string.IsNullOrWhiteSpace(v));

                    if (attr?.ListValueWithinBrackets ?? false)
                    {
                        values = values.Select(v => v.Substring(1));
                        values = values.Select(v => v.Substring(0, v.Length - 1));
                    }
                    collection.FromIniValues(values);
                }
                else
                    StringUtils.SetPropertyValue(kvPropertyValue, this, property);
            }
        }

        protected virtual string ToComplexINIValue(bool resultWithinBrackets)
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return string.Empty;

            var result = new StringBuilder();
            if (resultWithinBrackets)
                result.Append("(");

            var delimiter = "";
            foreach (var prop in this.Properties)
            {
                var attr = prop.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().FirstOrDefault();
                var propName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;
                var val = prop.GetValue(this);

                var collection = val as IIniValuesCollection;
                if (collection != null)
                {
                    result.Append(delimiter);
                    result.Append($"{propName}=");
                    if (attr?.ValueWithinBrackets ?? false)
                        result.Append("(");

                    var iniVals = collection.ToIniValues();
                    var delimiter2 = "";
                    foreach (var iniVal in iniVals)
                    {
                        result.Append(delimiter2);
                        if (attr?.ListValueWithinBrackets ?? false)
                            result.Append($"({iniVal})");
                        else
                            result.Append(iniVal);

                        delimiter2 = DELIMITER.ToString();
                    }

                    if (attr?.ValueWithinBrackets ?? false)
                        result.Append(")");

                    delimiter = DELIMITER.ToString();
                }
                else
                {
                    if ((attr?.ExcludeIfEmpty ?? false) && val is string && string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        Debug.WriteLine($"{propName} skipped, ExcludeIfEmpty = true and value is null or empty");
                    }
                    else if ((attr?.ExcludeIfFalse ?? false) && val is bool && !((bool)val))
                    {
                        Debug.WriteLine($"{propName} skipped, ExcludeIfFalse = true and value is false");
                    }
                    else
                    {
                        var propValue = StringUtils.GetPropertyValue(val, prop, attr?.QuotedString ?? true);

                        result.Append(delimiter);
                        if (!(attr?.ExcludePropertyName ?? false))
                            result.Append($"{propName}=");
                        if (attr?.ValueWithinBrackets ?? false)
                            result.Append("(");

                        result.Append(propValue);

                        if (attr?.ValueWithinBrackets ?? false)
                            result.Append(")");

                        delimiter = DELIMITER.ToString();
                    }
                }
            }

            if (resultWithinBrackets)
                result.Append(")");
            return result.ToString();
        }

        protected IEnumerable<string> SplitCollectionValues(string valueString, char delimiter)
        {
            if (string.IsNullOrWhiteSpace(valueString))
                return new string[0];

            // string any leading or trailing spaces
            var tempString = valueString.Trim();

            // check if any delimiters
            var total1 = tempString.Count(c => c.Equals(delimiter));
            if (total1 == 0)
                return new[] {tempString};

            var result = new List<string>();

            var bracketCount = 0;
            var startIndex = 0;
            for (var index = 0; index < tempString.Length; index++)
            {
                var charValue = tempString[index];
                if (charValue == '(')
                {
                    bracketCount++;
                    continue;
                }
                if (charValue == ')')
                {
                    bracketCount--;
                    continue;
                }
                if (charValue != delimiter || bracketCount != 0)
                    continue;

                result.Add(tempString.Substring(startIndex, index - startIndex));

                startIndex = index + 1;
            }

            result.Add(tempString.Substring(startIndex));

            return result;
        }

        public void Update(AggregateIniValue other)
        {
            if (other == null)
                return;

            GetPropertyInfos();
            other.GetPropertyInfos();

            foreach (var propInfo in this.Properties)
            {
                var otherPropInfo = other.Properties.FirstOrDefault(p => p.Name.Equals(propInfo.Name, StringComparison.OrdinalIgnoreCase));
                if (otherPropInfo == null)
                    continue;

                var val = otherPropInfo.GetValue(other);
                var propValue = StringUtils.GetPropertyValue(val, propInfo);

                StringUtils.SetPropertyValue(propValue, this, propInfo);
            }
        }
    }
}
