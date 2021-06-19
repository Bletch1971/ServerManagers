using System;

namespace ServerManagerTool.Common.Attibutes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AggregateIniValueEntryAttribute : Attribute
    {
        /// <summary>
        /// Attribute for the IniFile value
        /// </summary>
        /// <param name="key">The key of the value.  Defaults to the same name as the attributed field.</param>
        public AggregateIniValueEntryAttribute(string key = "")
        {
            this.Key = key;
        }

        /// <summary>
        /// The key of the value.
        /// </summary>
        public string Key;

        /// <summary>
        /// If true, the value will always be surrounded with brackets
        /// </summary>
        public bool ValueWithinBrackets;

        /// <summary>
        /// If true, the every list value will always be surrounded with brackets
        /// </summary>
        public bool ListValueWithinBrackets;

        /// <summary>
        /// Determines the number od brackets around the Value delimiter. Default 1, but will be higher for hierarchial values.
        /// </summary>
        public int BracketsAroundValueDelimiter = 1;

        /// <summary>
        /// If true, then the property will not be written if empty. This does not work for collections, only value types.
        /// </summary>
        public bool ExcludeIfEmpty;

        /// <summary>
        /// If true, then the property will not be written if false. This does not work for collections, only BOOLEAN types.
        /// </summary>
        public bool ExcludeIfFalse = false;

        /// <summary>
        /// If true, the value will always be written with quotes; otherwise without quotes.
        /// </summary>
        public bool QuotedString = true;

        /// <summary>
        /// If true, then the property name will not be written. This does not work for collections, only value types.
        /// </summary>
        public bool ExcludePropertyName = false;
    }
}
