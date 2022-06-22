using ServerManagerTool.Common.Enums;
using System;

namespace ServerManagerTool.Common.Attibutes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public abstract class BaseIniFileEntryAttribute : Attribute
    {
        /// <summary>
        /// Attribute for the IniFile serializer
        /// </summary>
        /// <param name="file">The file into which the setting should be serialized.</param>
        /// <param name="section">The section in the ini file.</param>
        /// <param name="category">The category of the setting.</param>
        /// <param name="key">The key within the section. Defaults to the same name as the attributed field.</param>
        protected BaseIniFileEntryAttribute(Enum file, Enum section, Enum category, string key = "")
        {
            this.File = file;
            this.Section = section;
            this.Category = category;
            this.Key = key;

            this.QuotedString = QuotedStringType.False;
            this.Multiline = false;
            this.MultilineSeparator = @"\n";
        }

        public Enum File { get; set; }

        /// <summary>
        /// The section of the ini file.
        /// </summary>
        public Enum Section { get; set; }

        /// <summary>
        /// The category of the setting.
        /// </summary>
        public Enum Category { get; set; }

        /// <summary>
        /// The key within the section.
        /// </summary>
        public string Key;

        /// <summary>
        /// Only write the attributed value if the value is different to the specified value.
        /// </summary>
        public object WriteIfNotValue;

        /// <summary>
        /// If true, the value of booleans will be inverted when read or written.
        /// </summary>
        public bool InvertBoolean;

        /// <summary>
        /// If true, will also write a true boolean value when the underlying field is non-default (or empty for strings), otherwise a false value will be written.
        /// </summary>
        public bool WriteBoolValueIfNonEmpty;

        /// <summary>
        /// If true, the value of booleans will be written as an integer (0 = false, 1 = true).
        /// </summary>
        public bool WriteBooleanAsInteger;

        /// <summary>
        /// Clear the section before writing this value.
        /// </summary>
        public bool ClearSection;

        /// <summary>
        /// Clear the section after processing this value and the section is empty.
        /// NOTE: DO NOT USE this setting for the standard setting sections, only use for custom sections or mod sections.
        /// </summary>
        public bool ClearSectionIfEmpty;

        /// <summary>
        /// If true, the value will always be written with quotes, if remove, the value will always be written without quotes even if added.
        /// </summary>
        public QuotedStringType QuotedString;

        /// <summary>
        /// Only write the attributed value if the named field is true.
        /// </summary>
        public string ConditionedOn;

        /// <summary>
        /// If true, the value will be treated as a multiline value.
        /// </summary>
        public bool Multiline;

        /// <summary>
        /// The new line separator to use when Multiline = True.
        /// </summary>
        public string MultilineSeparator;

        /// <summary>
        /// Clears the value when the named field is off, otherwise if on will skip the update. 
        /// NOTE: Use this for config fields that are updated by the server, while it is ruuning.
        /// </summary>
        public string ClearWhenOff;

        public bool IsCustom;
    }
}
