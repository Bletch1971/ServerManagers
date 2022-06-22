using ServerManagerTool.Common.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ServerManagerTool.Common.Utils
{
    public static class IniFileUtils
    {
        /// <summary>
        /// Retrieves all the keys and values for the specified section of an initialization file.
        /// </summary>
        /// <param name="file">The name of the initialization file.</param>
        /// <param name="sectionName">The name of the section in the initialization file.</param>
        /// <returns>A string array containing the key name and value pairs associated with the named section.</returns>
        public static IEnumerable<string> ReadSection(string file, string sectionName)
        {
            if (sectionName == null)
                return new string[0];

            var iniFile = ReadFromFile(file);
            return iniFile?.GetSection(sectionName)?.KeysToStringEnumerable() ?? new string[0];
        }

        /// <summary>
        /// Retrieves a string from the specified section in an initialization file.
        /// </summary>
        /// <param name="file">The name of the initialization file.</param>
        /// <param name="sectionName">The name of the section containing the key name.</param>
        /// <param name="keyName">The name of the key whose associated string is to be retrieved.</param>
        /// <param name="defaultValue">A default string. If the keyName key cannot be found in the initialization file, the default string is returned. If this parameter is NULL, the default is an empty string, "".</param>
        /// <returns></returns>
        public static string ReadValue(string file, string sectionName, string keyName, string defaultValue)
        {
            if (sectionName == null || keyName == null)
                return defaultValue ?? string.Empty;

            var iniFile = ReadFromFile(file);
            return iniFile?.GetSection(sectionName)?.GetKey(keyName)?.KeyValue ?? defaultValue ?? string.Empty;
        }

        /// <summary>
        /// Replaces the keys and values for the specified section in an initialization file.
        /// </summary>
        /// <param name="file">The name of the initialization file.</param>
        /// <param name="sectionName">The name of the section in which data is written.</param>
        /// <param name="keysValuePairs">An array of key names and associated values that are to be written to the named section.</param>
        /// <returns>True if the function succeeds; otherwise False.</returns>
        public static bool WriteSection(string file, string sectionName, IEnumerable<string> keysValuePairs)
        {
            if (sectionName == null)
                return false;

            var iniFile = ReadFromFile(file) ?? new IniFile();

            var result = iniFile.WriteSection(sectionName, keysValuePairs);
            if (!result)
                return false;

            return SaveToFile(file, iniFile);
        }

        /// <summary>
        /// Copies a string into the specified section of an initialization file.
        /// </summary>
        /// <param name="file">The name of the initialization file.</param>
        /// <param name="sectionName">The name of the section to which the string will be copied. If the section does not exist, it is created. The name of the section is case-independent; the string can be any combination of uppercase and lowercase letters.</param>
        /// <param name="keyName">The name of the key to be associated with a string. If the key does not exist in the specified section, it is created. If this parameter is NULL, the entire section, including all entries within the section, is deleted.</param>
        /// <param name="keyValue">A null-terminated string to be written to the file. If this parameter is NULL, the key pointed to by the keyName parameter is deleted.</param>
        /// <returns>True if the function succeeds; otherwise False.</returns>
        public static bool WriteValue(string file, string sectionName, string keyName, string keyValue)
        {
            if (sectionName == null)
                return false;

            var iniFile = ReadFromFile(file) ?? new IniFile();

            var result = iniFile.WriteKey(sectionName, keyName, keyValue);
            if (!result)
                return false;

            return SaveToFile(file, iniFile);
        }

        public static IniFile ReadFromFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return null;
            }

            if (!File.Exists(file))
            {
                return new IniFile();
            }

            var iniFile = new IniFile();

            using (StreamReader reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Trim();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                    {
                        continue;
                    }

                    var sectionName = string.Empty;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        sectionName = Regex.Match(line, @"(?<=^\[).*(?=\]$)").Value.Trim();
                    }

                    var section = iniFile.AddSection(sectionName);
                    if (section is null)
                    {
                        iniFile.AddKey(line);
                    }
                }

                reader.Close();
            }

            return iniFile;
        }

        public static IniFile ReadString(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new IniFile();

            var iniFile = new IniFile();

            var lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int index = 0; index < lines.Length; index++)
            {
                var line = lines[index].Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                var sectionName = Regex.Match(line, @"(?<=^\[).*(?=\]$)").Value.Trim();

                var section = iniFile.AddSection(sectionName);
                if (section != null)
                    continue;

                iniFile.AddKey(line);
            }

            return iniFile;
        }

        public static bool SaveToFile(string file, IniFile iniFile)
        {
            if (string.IsNullOrWhiteSpace(file) || iniFile == null)
                return false;

            using (StreamWriter writer = new StreamWriter(file, false))
            {
                foreach (var section in iniFile.Sections)
                {
                    writer.WriteLine($"[{section.SectionName}]");

                    foreach (var keyString in section.KeysToStringEnumerable())
                    {
                        writer.WriteLine(keyString);
                    }

                    writer.WriteLine();
                }

                writer.Close();
            }

            return true;
        }
    }
}
