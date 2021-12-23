using Newtonsoft.Json;
using ServerManagerTool.Common.Lib;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ServerManagerTool.Common.Utils
{
    public static class SettingsUtils
    {
        public static void BackupUserConfigSettings(ApplicationSettingsBase settings, string fileName, string settingsPath, string backupPath)
        {
            if (settings == null || string.IsNullOrWhiteSpace(fileName))
                return;

            var settingsFileName = Path.GetFileNameWithoutExtension(fileName);
            var settingsFileExt = Path.GetExtension(fileName);
            var settingsFile = IOUtils.NormalizePath(Path.Combine(settingsPath, $"{fileName}"));

            try
            {
                // save the settings file to a json settings file
                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new UserScopedSettingContractResolver(),
                };
                JsonUtils.SerializeToFile(settings, settingsFile, jsonSettings);
            }
            catch (Exception)
            {
                // do nothing, just exit
            }

            if (!string.IsNullOrWhiteSpace(backupPath))
            {
                // create a backup of the settings file
                var backupFile = IOUtils.NormalizePath(Path.Combine(backupPath, $"{settingsFileName}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}{settingsFileExt}"));

                try
                {
                    if (!Directory.Exists(backupPath))
                        Directory.CreateDirectory(backupPath);
                    File.Copy(settingsFile, backupFile);
                }
                catch (Exception)
                {
                    // do nothing, just exit
                }
            }
        }

        public static void DeleteBackupUserConfigFiles(string fileName, string backupPath, int interval)
        {
            var backupFileName = Path.GetFileNameWithoutExtension(fileName);
            var backupFileExt = Path.GetExtension(fileName);
            var backupFileFilter = $"{backupFileName}_*{backupFileExt}";
            var backupDateFilter = DateTime.Now.AddDays(-interval);

            try
            {
                Debug.WriteLine("Deleting old config backup files started...");

                var filesToDelete = new DirectoryInfo(backupPath).GetFiles(backupFileFilter).Where(f => f.LastWriteTime < backupDateFilter).ToArray();
                foreach (var fileToDelete in filesToDelete)
                {
                    try
                    {
                        fileToDelete.Delete();
                        Debug.WriteLine($"{fileToDelete.Name} was deleted, last updated {fileToDelete.CreationTime}.");
                    }
                    catch
                    {
                        // if unable to delete, do not bother
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting old config backup files.\r\n{ex.Message}");
            }
            finally
            {
                Debug.WriteLine("Deleting old config backup files finished.");
            }
        }

        public static void MigrateSettings(ApplicationSettingsBase settings, string settingsFile)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settingsFile) || !File.Exists(settingsFile))
                return;

            try
            {
                // read the json settings file to a settings file
                var jsonSettings = new JsonSerializerSettings
                {
                    //ContractResolver = new UserScopedSettingContractResolver(),
                };

                JsonUtils.PopulateFromFile(settingsFile, settings, jsonSettings);
            }
            catch (Exception)
            {
                // do nothing, just exit
            }
        }
    }
}
