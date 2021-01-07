using System;
using System.IO;

namespace ServerManagerTool.Common.Utils
{
    public static class VersionUtils
    {
        public static Version GetVersionFromFile(string versionFile)
        {
            if (!string.IsNullOrWhiteSpace(versionFile) && File.Exists(versionFile))
            {
                var fileValue = File.ReadAllText(versionFile);

                if (!string.IsNullOrWhiteSpace(fileValue))
                {
                    string versionString = fileValue.ToString();
                    if (versionString.IndexOf('.') == -1)
                        versionString = versionString + ".0";

                    if (Version.TryParse(versionString, out Version version))
                        return version;
                }
            }

            return new Version(0, 0);
        }
    }
}
