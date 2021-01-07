using System;
using System.Reflection;
using System.Xml;

namespace ServerManagerTool.Common.Utils
{
    public static class AppUtils
    {
        public static string GetDeployedVersion()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly();
                return GetDeployedVersion(assembly);
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetDeployedVersion(Assembly assembly)
        {
            if (assembly == null)
                return "Unknown";

            try
            {
                string executePath = new Uri(assembly.GetName().CodeBase).LocalPath;

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(executePath + ".manifest");

                var ns = new XmlNamespaceManager(xmlDoc.NameTable);
                ns.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");

                var xPath = "/asmv1:assembly/asmv1:assemblyIdentity/@version";
                var node = xmlDoc.SelectSingleNode(xPath, ns);

                return node.Value;
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
