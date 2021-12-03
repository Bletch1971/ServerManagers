using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace ServerManagerTool.Plugin.Common
{
    public static class ResourceUtils
    {
        public static string GetResourceString(ResourceDictionary resources, string inKey)
        {
            if (resources == null)
                throw new ArgumentNullException(nameof(resources), "parameter cannot be null.");
            if (string.IsNullOrWhiteSpace(inKey))
                throw new ArgumentNullException(nameof(inKey), "parameter cannot be null.");

            if (resources.Contains(inKey) && resources[inKey] is string)
            {
                var resourceString = resources[inKey].ToString();
                resourceString = resourceString.Replace("\\r", "\r");
                resourceString = resourceString.Replace("\\n", "\n");
                return resourceString;
            }
            return null;
        }

        public static void RemoveExceptResourceDictionary(Window window, string dictionaryName)
        {
            if (window == null || string.IsNullOrWhiteSpace(dictionaryName))
                return;

            var dictionariesToRemove = window.Resources.MergedDictionaries.Where(d => !d.Source.OriginalString.Contains(dictionaryName)).ToList();
            if (dictionariesToRemove != null)
            {
                foreach (var dictionaryToRemove in dictionariesToRemove)
                {
                    window.Resources.MergedDictionaries.Remove(dictionaryToRemove);
                }
            }
        }

        public static void RemoveExceptResourceDictionary(UserControl control, string dictionaryName)
        {
            if (control == null || string.IsNullOrWhiteSpace(dictionaryName))
                return;

            var dictionariesToRemove = control.Resources.MergedDictionaries.Where(d => !d.Source.OriginalString.Contains(dictionaryName)).ToList();
            if (dictionariesToRemove != null)
            {
                foreach (var dictionaryToRemove in dictionariesToRemove)
                {
                    control.Resources.MergedDictionaries.Remove(dictionaryToRemove);
                }
            }
        }

        public static void RemoveResourceDictionary(Window window, string dictionaryName)
        {
            if (window == null || string.IsNullOrWhiteSpace(dictionaryName))
                return;

            var dictionaryToRemove = window.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.Contains(dictionaryName));
            if (dictionaryToRemove != null)
            {
                window.Resources.MergedDictionaries.Remove(dictionaryToRemove);
            }
        }

        public static void RemoveResourceDictionary(UserControl control, string dictionaryName)
        {
            if (control == null || string.IsNullOrWhiteSpace(dictionaryName))
                return;

            var dictionaryToRemove = control.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.Contains(dictionaryName));
            if (dictionaryToRemove != null)
            {
                control.Resources.MergedDictionaries.Remove(dictionaryToRemove);
            }
        }

        public static void UpdateResourceDictionary(Window window, string languageCode)
        {
            if (window == null)
                return;

            RemoveExceptResourceDictionary(window, PluginHelper.LANGUAGECODE_FALLBACK);

            var assembly = Assembly.GetCallingAssembly();

            var resourcePath = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith($"{languageCode}.xaml"));
            if (string.IsNullOrWhiteSpace(resourcePath))
                return;

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    var resourceDictionary = XamlReader.Load(reader.BaseStream) as ResourceDictionary;
                    if (resourceDictionary != null)
                    {
                        window.Resources.MergedDictionaries.Add(resourceDictionary);
                    }
                }
            }
        }

        public static void UpdateResourceDictionary(UserControl control, string languageCode)
        {
            if (control == null)
                return;

            RemoveExceptResourceDictionary(control, PluginHelper.LANGUAGECODE_FALLBACK);

            var assembly = Assembly.GetCallingAssembly();

            var resourcePath = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith($"{languageCode}.xaml"));
            if (string.IsNullOrWhiteSpace(resourcePath))
                return;

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    var resourceDictionary = XamlReader.Load(reader.BaseStream) as ResourceDictionary;
                    if (resourceDictionary != null)
                    {
                        control.Resources.MergedDictionaries.Add(resourceDictionary);
                    }
                }
            }
        }
    }
}
