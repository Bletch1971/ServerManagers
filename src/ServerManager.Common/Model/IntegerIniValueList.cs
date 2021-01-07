using ServerManagerTool.Common.Interfaces;
using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ServerManagerTool.Common.Model
{
    public class IntegerIniValueList : IniValueList<int>, IIniValuesList
    {
        public IntegerIniValueList(string iniKeyName, Func<IEnumerable<int>> resetFunc) : 
            base(iniKeyName, resetFunc, (a, b) => a == b, m => m, ToIniValueInternal, FromIniValueInternal)
        {
        }

        public override bool IsArray => false;

        private static string ToIniValueInternal(int val)
        {
            return val.ToString(CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }

        private static int FromIniValueInternal(string iniVal)
        {
            return int.Parse(iniVal, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }

        public IEnumerable<string> ToIniValues(object excludeIfValue)
        {
            var excludeIfIntegerValue = excludeIfValue is int ? (int)excludeIfValue : int.MinValue;

            var values = new List<string>();
            if (string.IsNullOrWhiteSpace(IniCollectionKey))
                values.AddRange(this.Where(v => !EquivalencyFunc(v, excludeIfIntegerValue)).Select(d => ToIniValueInternal(d)));
            else
                values.AddRange(this.Where(v => !EquivalencyFunc(v, excludeIfIntegerValue)).Select(d => $"{this.IniCollectionKey}={ToIniValueInternal(d)}"));
            return values;
        }
    }
}
