using ServerManagerTool.Utils;
using System;
using System.IO;

namespace ServerManagerTool.Lib
{
    public class ModDetailExtended
    {
        public ModDetailExtended(string modId)
        {
            ModId = modId;
        }

        public string MapName { get; set; }

        private string ModId { get; set; }

        public string ModType { get; set; }

        public DateTime LastWriteTime { get; set; }

        public int LastTimeUpdated { get; set; }

        public long FolderSize { get; set; }

        public void PopulateExtended(string modsRootFolder)
        {
            try
            {
                FolderSize = 0;
                LastWriteTime = DateTime.MinValue;
                ModType = ModUtils.MODTYPE_UNKNOWN;
                MapName = string.Empty;

                if (string.IsNullOrWhiteSpace(modsRootFolder) || !Directory.Exists(modsRootFolder))
                    return;

                var modFileName = $"{ModId}.pak";
                var modFile = Path.Combine(modsRootFolder, modFileName);
                if (!string.IsNullOrWhiteSpace(modFile) && File.Exists(modFile))
                {
                    var file = new FileInfo(modFile);
                    LastWriteTime = file.LastWriteTime;
                    FolderSize += file.Length;
                    ModType = ModUtils.MODTYPE_MOD;
                }

                var timeFileName = $"{ModId}.txt";
                var modTimeFile = Path.Combine(modsRootFolder, timeFileName);
                if (!string.IsNullOrWhiteSpace(modTimeFile) && File.Exists(modTimeFile))
                {
                    LastTimeUpdated = ModUtils.GetModLatestTime(modTimeFile);
                }
            }
            catch
            {
                // do nothing
            }
        }
    }
}
