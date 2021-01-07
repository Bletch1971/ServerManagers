using ServerManagerTool.Common.Interfaces;
using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ServerManagerTool.Common.Model
{
    public class FloatIniValueList : IniValueList<float>, IIniValuesList
    {
        public FloatIniValueList(string iniKeyName, Func<IEnumerable<float>> resetFunc) : 
            base(iniKeyName, resetFunc, (a, b) => a == b, m => m, ToIniValueInternal, FromIniValueInternal)
        {
        }

        public override bool IsArray => false;

        private static string ToIniValueInternal(float val)
        {
            return val.ToString("0.0#########", CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }

        private static float FromIniValueInternal(string iniVal)
        {
            return float.Parse(iniVal, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }

        public IEnumerable<string> ToIniValues(object excludeIfValue)
        {
            var excludeIfFloatValue = excludeIfValue is float ? (float)excludeIfValue : float.NaN;

            var values = new List<string>();
            if (string.IsNullOrWhiteSpace(IniCollectionKey))
                values.AddRange(this.Where(v => !EquivalencyFunc(v, excludeIfFloatValue)).Select(d => ToIniValueInternal(d)));
            else
                values.AddRange(this.Where(v => !EquivalencyFunc(v, excludeIfFloatValue)).Select(d => $"{this.IniCollectionKey}={ToIniValueInternal(d)}"));
            return values;
        }
    }
}
