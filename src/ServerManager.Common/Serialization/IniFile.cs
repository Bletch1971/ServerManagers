using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerManagerTool.Common.Serialization
{
    public class IniFile
    {
        public IniFile()
        {
            Sections = new List<IniSection>();
        }

        public List<IniSection> Sections;

        public IniSection AddSection(string sectionName, bool allowEmptyName = false)
        {
            if (!allowEmptyName && string.IsNullOrWhiteSpace(sectionName))
                return null;

            var section = Sections.FirstOrDefault(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section == null)
            {
                section = new IniSection() { SectionName = sectionName };
                Sections.Add(section);
            }
            return section;
        }

        public IniKey AddKey(string keyName, string keyValue)
        {
            var section = Sections.LastOrDefault();
            if (section == null)
                section = AddSection(string.Empty, true);

            return section.AddKey(keyName, keyValue);
        }

        public IniKey AddKey(string keyValuePair)
        {
            var section = Sections.LastOrDefault();
            if (section == null)
                section = AddSection(string.Empty, true);

            return section.AddKey(keyValuePair);
        }

        public IniSection GetSection(string sectionName)
        {
            return Sections?.FirstOrDefault(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
        }

        public IniKey GetKey(string sectionName, string keyName)
        {
            return GetSection(sectionName)?.GetKey(keyName);
        }

        public void RemoveSection(string sectionName)
        {
            var section = GetSection(sectionName);
            RemoveSection(section);
        }

        public void RemoveSection(IniSection section)
        {
            if (Sections.Contains(section))
                Sections.Remove(section);
        }

        public bool WriteSection(string sectionName, IEnumerable<string> keysValuePairs)
        {
            if (sectionName == null)
                return false;

            var result = true;

            // get the section.
            var section = GetSection(sectionName);

            // check if the section exists.
            if (section != null)
            {
                // delete the section.
                RemoveSection(section);
            }

            // create the section.
            section = AddSection(sectionName);

            if (section != null)
            {
                foreach (var key in keysValuePairs)
                {
                    section.AddKey(key);
                }
            }

            return result;
        }

        public bool WriteKey(string sectionName, string keyName, string keyValue)
        {
            if (sectionName == null)
                return false;

            var result = true;

            // get the section.
            var section = GetSection(sectionName);

            // check if the section exists.
            if (section == null)
            {
                // section does not exist, check if keyname is NULL.
                if (keyName == null)
                {
                    // do nothing, the section does not exist and does not need to be removed.
                }
                else
                {
                    // create the section.
                    section = AddSection(sectionName);
                }
            }
            else
            {
                // section does exists, check if the keyname is NULL.
                if (keyName == null)
                {
                    // keyname is NULL, we need to delete the section.
                    RemoveSection(section);

                    // reset the section variable.
                    section = null;
                }
            }

            // check if the section exists.
            if (section != null)
            {
                // get the key.
                var key = section.GetKey(keyName);

                // check if the key exists.
                if (key == null)
                {
                    // key does not exist, check if keyvalue is NULL.
                    if (keyValue == null)
                    {
                        // do nothing, the key does not exist and does not need to be removed.
                    }
                    else
                    {
                        // create the key.
                        key = section.AddKey(keyName, keyValue);
                    }
                }
                else
                {
                    // key does exists, check if the keyvalue is NULL.
                    if (keyValue == null)
                    {
                        // keyvalue is NULL, we need to delete the key and exit.
                        section.RemoveKey(key);

                        // reset the key variable.
                        key = null;
                    }
                    else
                    {
                        // update the keyvalue.
                        key.KeyValue = keyValue;
                    }
                }
            }

            return result;
        }

        public string ToOutputString()
        {
            var result = new StringBuilder();

            foreach (var section in Sections)
            {
                result.AppendLine($"[{section.SectionName}]");

                foreach (var keyString in section.KeysToStringEnumerable())
                {
                    result.AppendLine(keyString);
                }

                result.AppendLine();
            }

            return result.ToString();
        }
    }
}
