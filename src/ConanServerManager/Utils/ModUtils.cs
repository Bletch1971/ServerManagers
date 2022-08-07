using ServerManagerTool.Common;
using ServerManagerTool.Common.Extensions;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServerManagerTool.Utils
{
    public static class ModUtils
    {
        public const string MODTYPE_UNKNOWN = "0";
        public const string MODTYPE_MOD = "1";
        public const string MODTYPE_MAP = "2";

        public static void CopyMod(string sourceFolder, string destinationFolder, string modId, ProgressDelegate progressCallback)
        {
            if (string.IsNullOrWhiteSpace(sourceFolder))
                throw new DirectoryNotFoundException($"Source folder must be specified.");
            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"Source folder was not found.\r\n{sourceFolder}");
            if (string.IsNullOrWhiteSpace(destinationFolder))
                throw new DirectoryNotFoundException($"Destination folder must be specified.");

            var modFileName = $"{modId}.pak";
            var modFile = IOUtils.NormalizePath(Path.Combine(destinationFolder, modFileName));
            var timeFileName = $"{modId}.txt";
            var timeFile = IOUtils.NormalizePath(Path.Combine(destinationFolder, timeFileName));

            progressCallback?.Invoke(0, "Deleting existing mod files.");

            // delete the server mod file.
            if (File.Exists(modFile))
                File.Delete(modFile);
            if (File.Exists(timeFile))
                File.Delete(timeFile);

            progressCallback?.Invoke(0, "Copying mod files.");

            if (string.IsNullOrWhiteSpace(destinationFolder) || !Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            // update the mod files from the cache.
            foreach (var sourceFile in Directory.GetFiles(sourceFolder, "*.pak", SearchOption.TopDirectoryOnly))
            {
                File.Copy(sourceFile, modFile, true);
            }

            // copy the last updated file.
            var fileName = IOUtils.NormalizePath(Path.Combine(sourceFolder, Config.Default.LastUpdatedTimeFile));
            if (File.Exists(fileName))
            {
                progressCallback?.Invoke(0, "Copying mod version file.");

                File.Copy(fileName, timeFile, true);
            }
        }

        public static void CreateModListFile(string installDirectory, List<string> modIdList)
        {
            if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
                return;

            // get the folder/file details
            var modRootFolder = GetModRootPath(installDirectory);
            var modListFileName = Config.Default.ServerModListFile;
            var modListFile = IOUtils.NormalizePath(Path.Combine(modRootFolder, modListFileName));

            // check if any mods to write
            if (modIdList == null || modIdList.Count == 0)
            {
                // no mods, check if foldler exists
                if (!Directory.Exists(modRootFolder))
                    return;

                // check if the file exists, if so then delete it.
                if (File.Exists(modListFile))
                    File.Delete(modListFile);
            }
            else
            {
                // check if the folder exists, if not then create it.
                if (!Directory.Exists(modRootFolder))
                    Directory.CreateDirectory(modRootFolder);

                // get the a list of the mod file into include in the mod file
                var modFileItems = modIdList.Select(m => $"{m}.pak");
                // create the mod file.
                File.WriteAllLines(modListFile, modFileItems);
            }
        }

        public static string GetLatestModCacheTimeFile(string modId, string appId) => IOUtils.NormalizePath(Path.Combine(GetModCachePath(modId, appId), Config.Default.LastUpdatedTimeFile));

        public static string GetLatestModTimeFile(string installDirectory, string modId) => IOUtils.NormalizePath(Path.Combine(installDirectory, Config.Default.ServerModsRelativePath, $"{modId}.txt"));

        public static string GetMapName(string serverMap)
        {
            if (string.IsNullOrWhiteSpace(serverMap))
                return string.Empty;

            return serverMap.Trim();
        }

        public static string GetModCachePath(string modId, string appId)
        {
            var workshopPath = string.Format(Config.Default.AppSteamWorkshopFolderRelativePath, appId);
            return IOUtils.NormalizePath(Path.Combine(Config.Default.DataPath, CommonConfig.Default.SteamCmdRelativePath, workshopPath, modId));
        }

        public static List<string> GetModIdList(string modIds)
        {
            if (string.IsNullOrWhiteSpace(modIds))
                return new List<string>();

            return modIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static int GetModLatestTime(string timeFile)
        {
            try
            {
                if (!File.Exists(timeFile))
                    return 0;

                var value = File.ReadAllText(timeFile);

                int unixTime;
                return int.TryParse(value, out unixTime) ? unixTime : 0;
            }
            catch
            {
                return 0;
            }
        }

        public static string GetModRootPath(string installDirectory) => IOUtils.NormalizePath(Path.Combine(installDirectory, Config.Default.ServerModsRelativePath));

        public static string GetModPath(string installDirectory, string modId) => GetModRootPath(installDirectory);

        public static string GetSteamManifestFile(string installDirectory, string appIdServer)
        {
            var fileName = string.Format(Config.Default.AppSteamManifestFile, appIdServer);
            return IOUtils.NormalizePath(Path.Combine(installDirectory, Config.Default.SteamManifestFolderRelativePath, fileName));
        }

        public static string GetSteamWorkshopFile(string appId)
        {
            var fileName = string.Format(Config.Default.AppSteamWorkshopFile, appId);
            return IOUtils.NormalizePath(Path.Combine(Config.Default.DataPath, CommonConfig.Default.SteamCmdRelativePath, Config.Default.SteamWorkshopFolderRelativePath, fileName));
        }

        public static int GetSteamWorkshopLatestTime(string workshopFile, string modId)
        {
            try
            {
                var result = SteamUtils.ReadSteamCmdAppWorkshopFile(workshopFile);
                if (result == null)
                    return 0;

                var detail = result.WorkshopItemDetails.FirstOrDefault(v => v.publishedfileid.Equals(modId));
                if (detail == null)
                    return 0;

                int unixTime;
                return int.TryParse(detail.timeupdated, out unixTime) ? unixTime : 0;
            }
            catch
            {
                return 0;
            }
        }

        public static bool IsOfficialMod(string modId)
        {
            switch (modId)
            {
                default:
                    return false;
            }
        }

        public static List<string> ReadModListFile(string installDirectory)
        {
            if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
                return new List<string>();

            // get the folder/file details
            var modRootFolder = GetModRootPath(installDirectory);
            var modListFileName = Config.Default.ServerModListFile;
            var modListFile = IOUtils.NormalizePath(Path.Combine(modRootFolder, modListFileName));

            // check if the folder/file exists
            if (!Directory.Exists(modRootFolder) || !File.Exists(modListFile))
                return new List<string>();

            var modFiles = File.ReadAllLines(modListFile);
            return modFiles
                .Select(m => Path.GetFileNameWithoutExtension(m))
                .Where(m => m.IsNumeric())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();
        }

        public static List<string> ValidateModList(List<string> modIdList)
        {
            // remove all duplicate mod ids.
            var newModIdList = modIdList.Distinct().ToList();

            // remove any official mods.

            return newModIdList;
        }
    }
}
