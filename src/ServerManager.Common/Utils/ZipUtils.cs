using Ionic.Zip;
using ServerManagerTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServerManagerTool.Common.Utils
{
    public static class ZipUtils
    {
        public static bool DoesFileExist(string zipFile, string entryName)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (string.IsNullOrWhiteSpace(entryName))
                throw new ArgumentNullException(nameof(entryName));

            if (!File.Exists(zipFile))
                throw new FileNotFoundException();

            using (var zip = ZipFile.Read(zipFile))
            {
                return zip.Entries.Any(e => Path.GetFileName(e.FileName).Equals(entryName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static int ExtractAFile(string zipFile, string entryName, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (string.IsNullOrWhiteSpace(entryName))
                throw new ArgumentNullException(nameof(entryName));
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentNullException(nameof(destinationPath));

            if (!File.Exists(zipFile))
                throw new FileNotFoundException();
            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            using (var zip = ZipFile.Read(zipFile))
            {
                var selection = zip.Entries.Where(e => Path.GetFileName(e.FileName).Equals(entryName, StringComparison.OrdinalIgnoreCase));

                foreach (var entry in selection)
                {
                    entry.Extract(destinationPath, ExtractExistingFileAction.OverwriteSilently);
                }

                return selection.Count();
            }
        }

        public static int ExtractAllFiles(string zipFile, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentNullException(nameof(destinationPath));

            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            using (var zip = ZipFile.Read(zipFile))
            {
                zip.ExtractAll(destinationPath, ExtractExistingFileAction.OverwriteSilently);

                return zip.Entries.Count;
            }
        }

        public static void UpdateFiles(string zipFile, IEnumerable<string> filesToZip, string comment = "", bool preserveDirHierarchy = true, string directoryPathInArchive = "")
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));

            if (filesToZip is null || filesToZip.IsEmpty())
                return;

            if (!File.Exists(zipFile))
            {
                ZipFiles(zipFile, filesToZip, comment, preserveDirHierarchy, directoryPathInArchive);
            }
            else
            {
                using (var zip = ZipFile.Read(zipFile))
                {
                    zip.AddFiles(filesToZip.Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f)), preserveDirHierarchy, directoryPathInArchive);

                    if (!string.IsNullOrWhiteSpace(comment))
                        zip.Comment = comment;

                    zip.Save();
                }
            }
        }

        public static void ZipAFile(string zipFile, string entryName, string content)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (string.IsNullOrWhiteSpace(entryName))
                throw new ArgumentNullException(nameof(entryName));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content));

            if (!File.Exists(zipFile))
            {
                using (var zip = new ZipFile())
                {
                    zip.AddEntry(entryName, content);

                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;

                    zip.Save(zipFile);
                }
            }
            else
            {
                using (var zip = ZipFile.Read(zipFile))
                {
                    zip.AddEntry(entryName, content);

                    zip.Save();
                }
            }
        }

        public static void ZipAFile(string zipFile, string entryName, string content, string comment)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (string.IsNullOrWhiteSpace(entryName))
                throw new ArgumentNullException(nameof(entryName));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content));

            if (!File.Exists(zipFile))
            {
                using (var zip = new ZipFile())
                {
                    zip.AddEntry(entryName, File.ReadAllBytes(content));

                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
                    if (!string.IsNullOrWhiteSpace(comment))
                        zip.Comment = comment;

                    zip.Save(zipFile);
                }
            }
            else
            {
                using (var zip = ZipFile.Read(zipFile))
                {
                    zip.AddEntry(entryName, File.ReadAllBytes(content));

                    if (!string.IsNullOrWhiteSpace(comment))
                        zip.Comment = comment;

                    zip.Save();
                }
            }
        }

        public static void ZipFiles(string zipFile, IEnumerable<string> filesToZip, string comment = "", bool preserveDirHierarchy = true, string directoryPathInArchive = "")
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));

            if (filesToZip is null || filesToZip.IsEmpty())
                throw new ArgumentNullException(nameof(filesToZip));

            using (var zip = new ZipFile(zipFile))
            {
                zip.AddFiles(filesToZip.Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f)), preserveDirHierarchy, directoryPathInArchive);

                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
                if (!string.IsNullOrWhiteSpace(comment))
                    zip.Comment = comment;

                zip.Save();
            }
        }
    }
}
