using Newtonsoft.Json;
using ServerManagerTool.Common.Lib;
using System;
using System.IO;
using System.Linq;

namespace ServerManagerTool.Common.Utils
{
    public static class SettingsUtils
    {
        public static void BackupUserConfigSettings(System.Configuration.ApplicationSettingsBase settings, string fileName, string settingsPath, string backupPath)
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
                var backupFile = IOUtils.NormalizePath(Path.Combine(backupPath, $"{settingsFileName}_{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")}{settingsFileExt}"));

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

                try
                {
                    var filesToDelete = new DirectoryInfo(backupPath).GetFiles($"{settingsFileName}_*{settingsFileExt}").Where(f => f.LastWriteTimeUtc.AddDays(7) < DateTime.UtcNow);
                    foreach (var fileToDelete in filesToDelete)
                    {
                        try
                        {
                            fileToDelete.Delete();
                        }
                        catch (Exception)
                        {
                            // do nothing, just exit
                        }
                    }
                }
                catch (Exception)
                {
                    // do nothing, just exit
                }
            }
        }

        public static void MigrateSettings(System.Configuration.ApplicationSettingsBase settings, string settingsFile)
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
