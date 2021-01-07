using System;
using System.IO;
using System.Reflection;

namespace ServerManagerTool.Common.Utils
{
    public static class AssemblyUtils
    {
        public static string GetAttributePropertyValue(Assembly assembly, Type attributeType, string propertyName)
        {
            var attributes = (Attribute[])assembly.GetCustomAttributes(attributeType, true);
            if (attributes.Length > 0)
            {
                PropertyInfo property = attributeType.GetProperty(propertyName, typeof(string));
                if (property != null)
                {
                    //Get value on first attribute.
                    return (string)property.GetValue(attributes[0], null);
                }
            }
            return "?";
        }

        public static string GetBuildDate()
        {
            return GetBuildDate(Assembly.GetEntryAssembly());
        }
        public static string GetBuildDate(Assembly assembly)
        {
            if (assembly != null && assembly.Location != null)
            {
                return File.GetLastWriteTime(assembly.Location).ToString();
            }
            return string.Empty;
        }

        public static string GetCompanyName()
        {
            return GetCompanyName(Assembly.GetEntryAssembly());
        }
        public static string GetCompanyName(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            if (attributes.Length > 0)
            {
                var attribute = (AssemblyCompanyAttribute)attributes[0];
                if (!string.IsNullOrWhiteSpace(attribute.Company))
                {
                    return attribute.Company;
                }
            }
            return string.Empty;
        }

        public static string GetCopyrightText()
        {
            return GetCopyrightText(Assembly.GetEntryAssembly());
        }
        public static string GetCopyrightText(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            if (attributes.Length > 0)
            {
                var attribute = (AssemblyCopyrightAttribute)attributes[0];
                if (!string.IsNullOrWhiteSpace(attribute.Copyright))
                {
                    return attribute.Copyright;
                }
            }
            return string.Empty;
        }

        public static string GetDescription()
        {
            return GetDescription(Assembly.GetEntryAssembly());
        }
        public static string GetDescription(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                var attribute = (AssemblyDescriptionAttribute)attributes[0];
                if (!string.IsNullOrWhiteSpace(attribute.Description))
                {
                    return attribute.Description;
                }
            }
            return string.Empty;
        }

        public static string GetFileVersion()
        {
            return GetFileVersion(Assembly.GetEntryAssembly());
        }
        public static string GetFileVersion(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
            if (attributes.Length > 0)
            {
                var attribute = (AssemblyFileVersionAttribute)attributes[0];
                if (!string.IsNullOrWhiteSpace(attribute.Version))
                {
                    return attribute.Version;
                }
            }
            return string.Empty;
        }

        public static string GetProductName()
        {
            return GetProductName(Assembly.GetEntryAssembly());
        }
        public static string GetProductName(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (attributes.Length > 0)
            {
                var attribute = (AssemblyProductAttribute)attributes[0];
                if (!string.IsNullOrWhiteSpace(attribute.Product))
                {
                    return attribute.Product;
                }
            }
            return string.Empty;
        }

        public static string GetTitle()
        {
            return GetTitle(Assembly.GetEntryAssembly());
        }
        public static string GetTitle(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                var attribute = (AssemblyTitleAttribute)attributes[0];
                if (!string.IsNullOrWhiteSpace(attribute.Title))
                {
                    return attribute.Title;
                }
            }
            // if there is no title attribute or if the title attribute was an empty string, then return the .exe name
            return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
        }

        public static string GetTrademark()
        {
            return GetTrademark(Assembly.GetEntryAssembly());
        }
        public static string GetTrademark(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyTrademarkAttribute), false);
            if (attributes.Length > 0)
            {
                var attribute = (AssemblyTrademarkAttribute)attributes[0];
                if (!string.IsNullOrWhiteSpace(attribute.Trademark))
                {
                    return attribute.Trademark;
                }
            }
            return string.Empty;
        }

        public static string GetVersion()
        {
            return GetVersion(Assembly.GetEntryAssembly());
        }
        public static string GetVersion(Assembly assembly)
        {
            return assembly.GetName().Version.ToString();
        }
    }
}
