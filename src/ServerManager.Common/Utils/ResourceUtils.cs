using System;
using System.Windows;

namespace ServerManagerTool.Common.Utils
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
    }
}
