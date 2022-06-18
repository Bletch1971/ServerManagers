using System;

namespace ServerManagerTool.Common.Extensions
{
    public static class StringExtensions
    {
        public static bool Contains(this string value, string substring, StringComparison comparisonType)
        {
            if (substring == null)
                throw new ArgumentNullException(nameof(substring), $"{nameof(substring)} cannot be null.");
            if (!Enum.IsDefined(typeof(StringComparison), comparisonType))
                throw new ArgumentException($"{nameof(comparisonType)} is not a member of StringComparison", nameof(comparisonType));

            return value.IndexOf(substring, comparisonType) >= 0;
        }
    }
}
