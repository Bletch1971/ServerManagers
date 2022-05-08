using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ServerManagerTool.Common.Utils
{
    public static class IOUtils
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        public static bool IsUnc(string path) => new Uri(path).IsUnc;

        public static string NormalizeFolder(string path)
        {
            var newPath = path.TrimEnd('\\') + "\\";

            if (IsUnc(newPath))
            {
                return newPath.TrimEnd('\\');
            }

            var root = Path.GetPathRoot(newPath);
            if (!root.EndsWith("\\"))
                root += "\\";

            if (!string.Equals(root, newPath, StringComparison.OrdinalIgnoreCase))
            {
                newPath = newPath.TrimEnd('\\');
            }

            return newPath;
        }

        public static string NormalizePath(string path) => 
            Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToLowerInvariant();

        public static bool Unblock(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }
    }
}
