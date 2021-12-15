using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerManagerTool.Common.Serialization
{
    public class IniSection
    {
        public IniSection()
        {
            SectionName = string.Empty;
            Keys = new List<IniKey>();
        }

        public string SectionName;
        public List<IniKey> Keys;

        public IniKey AddKey(string keyName, string keyValue)
        {
            var key = new IniKey() { KeyName = keyName, KeyValue = keyValue };
            Keys.Add(key);
            return key;
        }

        public IniKey AddKey(string keyValuePair)
        {
            var parts = keyValuePair?.Split(new[] { '=' }, 2) ?? new string[1];

            if (string.IsNullOrWhiteSpace(parts[0]))
                return null;

            var key = new IniKey() { KeyName = parts[0] };
            if (parts.Length > 1)
                key.KeyValue = parts[1];
            Keys.Add(key);
            return key;
        }

        public IniKey GetKey(string keyName)
        {
            return Keys?.FirstOrDefault(s => s.KeyName.Equals(keyName, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<string> KeysToStringEnumerable()
        {
            return Keys.Select(k => k.ToString());
        }

        public void RemoveKey(string keyName)
        {
            var key = GetKey(keyName);
            RemoveKey(key);
        }

        public void RemoveKey(IniKey key)
        {
            if (Keys.Contains(key))
                Keys.Remove(key);
        }

        public override string ToString()
        {
            return $"[{SectionName}]";
        }
    }
}
