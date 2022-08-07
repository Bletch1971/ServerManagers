using Ionic.Zlib;
using ServerManagerTool.Common.Lib;
using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServerManagerTool.Utils
{
    public static class ModUtils
    {
        public const string MODTYPE_UNKNOWN = "0";
        public const string MODTYPE_MAP = "2";
        public const string MODTYPE_MAPEXT = "4";
        public const string MODTYPE_MOD = "1";
        public const string MODTYPE_TOTCONV = "3";

        private static List<string> OfficialMods { get; set; } = new List<string>();

        private class FCompressedChunkInfo
        {
            public const uint LOADING_COMPRESSION_CHUNK_SIZE = 131072U;
            public const uint PACKAGE_FILE_TAG = 2653586369U;
            public const uint PACKAGE_FILE_TAG_SWAPPED = 3246598814U;

            public long CompressedSize;
            public long UncompressedSize;

            public void Serialize(BinaryReader reader)
            {
                CompressedSize = reader.ReadInt64();
                UncompressedSize = reader.ReadInt64();
            }
        }

        public static void AddOfficialMod(string modId)
        {
            if (OfficialMods == null)
                OfficialMods = new List<string>();

            if (!string.IsNullOrWhiteSpace(modId) && !OfficialMods.Contains(modId))
            {
                OfficialMods.Add(modId);
            }
        }

        public static void AddOfficialMods(List<string> modIds)
        {
            if (OfficialMods == null)
                OfficialMods = new List<string>();

            if (modIds != null)
            {
                var modIdsToAdd = modIds.Where(m => !string.IsNullOrWhiteSpace(m) && !OfficialMods.Contains(m)).Distinct();
                if (modIdsToAdd.Any())
                {
                    OfficialMods.AddRange(modIdsToAdd);
                }
            }
        }

        public static void CopyMod(string sourceFolder, string destinationFolder, string modId, ProgressDelegate progressCallback)
        {
            if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"Source folder was not found.\r\n{sourceFolder}");

            var modSourceFolder = sourceFolder;

            progressCallback?.Invoke(0, "Reading mod base information.");

            var fileName = IOUtils.NormalizePath(Path.Combine(modSourceFolder, "mod.info"));
            var list = new List<string>();
            ParseBaseInformation(fileName, list);

            progressCallback?.Invoke(0, "Reading mod meta information.");

            fileName = IOUtils.NormalizePath(Path.Combine(modSourceFolder, "modmeta.info"));
            var metaInformation = new Dictionary<string, string>();
            if (ParseMetaInformation(fileName, metaInformation))
                modSourceFolder = IOUtils.NormalizePath(Path.Combine(modSourceFolder, "WindowsNoEditor"));

            var modFile = $"{destinationFolder}.mod";

            progressCallback?.Invoke(0, "Deleting existing mod files.");

            // delete the server mod folder and mod file.
            if (Directory.Exists(destinationFolder))
                Directory.Delete(destinationFolder, true);
            if (File.Exists(modFile))
                File.Delete(modFile);

            progressCallback?.Invoke(0, "Copying mod files.");

            // update the mod files from the cache.
            var flag = Copy(modSourceFolder, destinationFolder, true);

            if (metaInformation.Count == 0 && flag)
                metaInformation["ModType"] = "1";

            progressCallback?.Invoke(0, "Creating mod file.");

            // create the mod file.
            WriteModFile(modFile, modId, metaInformation, list);

            // copy the last updated file.
            fileName = IOUtils.NormalizePath(Path.Combine(sourceFolder, Config.Default.LastUpdatedTimeFile));
            if (File.Exists(fileName))
            {
                progressCallback?.Invoke(0, "Copying mod version file.");

                var tempFile = IOUtils.NormalizePath(fileName.Replace(sourceFolder, destinationFolder));
                File.Copy(fileName, tempFile, true);
            }
        }

        public static bool Copy(string sourceFolder, string destinationFolder, bool copySubFolders)
        {
            if (!Directory.Exists(sourceFolder))
                return false;

            var flag = false;

            foreach (var sourceFile in Directory.GetFiles(sourceFolder, "*.*", copySubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                var modFile = IOUtils.NormalizePath(sourceFile.Replace(sourceFolder, destinationFolder));
                var modFilePath = Path.GetDirectoryName(modFile);

                if (!Directory.Exists(modFilePath))
                    Directory.CreateDirectory(modFilePath);

                if (Path.GetFileNameWithoutExtension(sourceFile).Contains("PrimalGameData"))
                    flag = true;

                Copy(sourceFile, modFilePath);
            }

            return flag;
        }

        public static void Copy(string sourceFile, string destinationFolder)
        {
            string fileExtension = Path.GetExtension(sourceFile).ToUpper();

            if (string.Compare(fileExtension, ".uncompressed_size", StringComparison.OrdinalIgnoreCase) != 0)
            {
                string tempFile = Path.Combine(destinationFolder, Path.GetFileName(sourceFile));

                if (string.Compare(fileExtension, ".z", StringComparison.OrdinalIgnoreCase) == 0)
                    UE4ChunkUnzip(sourceFile, tempFile.Substring(0, tempFile.Length - 2));
                else
                    File.Copy(sourceFile, tempFile, true);
            }
        }

        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            TimeSpan timespan = dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return timespan.TotalSeconds;
        }

        public static string GetLatestModCacheTimeFile(string modId, string appId) => IOUtils.NormalizePath(Path.Combine(GetModCachePath(modId, appId), Config.Default.LastUpdatedTimeFile));

        public static string GetLatestModTimeFile(string installDirectory, string modId) => IOUtils.NormalizePath(Path.Combine(installDirectory, Config.Default.ServerModsRelativePath, modId, Config.Default.LastUpdatedTimeFile));

        public static string GetMapModId(string serverMap)
        {
            if (string.IsNullOrWhiteSpace(serverMap))
                return string.Empty;

            // split the map string into parts, using the '/' separator.
            var parts = serverMap.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            long mapModId;
            if (parts.Length == 1 && long.TryParse(parts[0], out mapModId))
                return mapModId.ToString();

            // check if any parts were returned.
            if (parts.Length != 4)
                return string.Empty;

            // check if the first two parts match what is expected.
            if (!parts[0].Equals("game", StringComparison.OrdinalIgnoreCase) || !parts[1].Equals("mods", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            // return the third part, this should be the mod number.
            return parts[2].ToString();
        }

        public static string GetMapName(string serverMap)
        {
            if (string.IsNullOrWhiteSpace(serverMap))
                return string.Empty;

            // split the map string into parts, using the '/' separator.
            var parts = serverMap.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // check if any parts were returned.
            if (parts.Length == 1)
                return serverMap;
            if (parts.Length != 4)
                return string.Empty;

            // check if the first two parts match what is expected.
            if (!parts[0].Equals("game", StringComparison.OrdinalIgnoreCase) || !parts[1].Equals("mods", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            // return the fourth part, this should be the map name.
            return parts[3];
        }

        public static string GetMapName(string installDirectory, string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return string.Empty;

            var modFolder = GetModPath(installDirectory, modId);
            var modFile = $"{modFolder}.mod";

            if (!File.Exists(modFile))
                return string.Empty;

            Dictionary<string, string> metaInformation;
            List<string> mapNames;
            ReadModFile(modFile, out modId, out metaInformation, out mapNames);

            return mapNames != null && mapNames.Count > 0 ? mapNames[0] : string.Empty;
        }

        public static string GetModCachePath(string modId, string appId)
        {
            var workshopPath = string.Format(Config.Default.AppSteamWorkshopFolderRelativePath, appId);
            return IOUtils.NormalizePath(Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir, workshopPath, modId));
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

        public static string GetModPath(string installDirectory, string modId) => IOUtils.NormalizePath(Path.Combine(installDirectory, Config.Default.ServerModsRelativePath, modId));

        public static string GetModType(string installDirectory, string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return string.Empty;

            var modFolder = GetModPath(installDirectory, modId);
            var modFile = $"{modFolder}.mod";

            ReadModFile(modFile, out _, out Dictionary<string, string> metaInformation, out _);

            return metaInformation != null && metaInformation.ContainsKey("ModType") ? metaInformation["ModType"] : MODTYPE_UNKNOWN;
        }

        public static string GetSteamManifestFile(string installDirectory, string appIdServer)
        {
            var fileName = string.Format(Config.Default.AppSteamManifestFile, appIdServer);
            return IOUtils.NormalizePath(Path.Combine(installDirectory, Config.Default.SteamManifestFolderRelativePath, fileName));
        }

        public static string GetSteamWorkshopFile(string appId)
        {
            var fileName = string.Format(Config.Default.AppSteamWorkshopFile, appId);
            return IOUtils.NormalizePath(Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir, Config.Default.SteamWorkshopFolderRelativePath, fileName));
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
            if (OfficialMods == null || OfficialMods.Count == 0)
                return false;

            return OfficialMods.Contains(modId);
        }

        public static bool ParseBaseInformation(string fileName, List<string> mapNames)
        {
            if (!File.Exists(fileName))
                return false;

            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                string readString1;
                ReadUE4String(reader, out readString1);

                int num = reader.ReadInt32();
                for (int index = 0; index < num; ++index)
                {
                    string readString2;
                    ReadUE4String(reader, out readString2);
                    mapNames.Add(readString2);
                }
            }
            return true;
        }

        public static bool ParseMetaInformation(string fileName, Dictionary<string, string> metaInformation)
        {
            if (!File.Exists(fileName))
                return false;

            using (BinaryReader binaryReader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                int num = binaryReader.ReadInt32();
                for (int index1 = 0; index1 < num; ++index1)
                {
                    string index2 = string.Empty;
                    int count1 = binaryReader.ReadInt32();
                    bool flag1 = false;
                    if (count1 < 0)
                    {
                        flag1 = true;
                        count1 = -count1;
                    }
                    if (!flag1 && count1 > 0)
                    {
                        byte[] bytes = binaryReader.ReadBytes(count1);
                        index2 = Encoding.UTF8.GetString(bytes, 0, bytes.Length - 1);
                    }
                    string str = string.Empty;
                    int count2 = binaryReader.ReadInt32();
                    bool flag2 = false;
                    if (count2 < 0)
                    {
                        flag2 = true;
                        count2 = -count2;
                    }
                    if (!flag2 && count2 > 0)
                    {
                        byte[] bytes = binaryReader.ReadBytes(count2);
                        str = Encoding.UTF8.GetString(bytes, 0, bytes.Length - 1);
                    }
                    metaInformation[index2] = str;
                }
            }
            return true;
        }

        public static void ReadModFile(string fileName, out string modId, out Dictionary<string, string> metaInformation, out List<string> mapNames)
        {
            modId = null;
            metaInformation = new Dictionary<string, string>();
            mapNames = new List<string>();

            if (!File.Exists(fileName))
                return;

            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                ulong num1 = reader.ReadUInt64();
                modId = num1.ToString();

                string readString1;
                ReadUE4String(reader, out readString1);
                string readString2;
                ReadUE4String(reader, out readString2);

                int count1 = reader.ReadInt32();
                for (int index = 0; index < count1; ++index)
                {
                    string readString3;
                    ReadUE4String(reader, out readString3);
                    mapNames.Add(readString3);
                }

                uint num2 = reader.ReadUInt32();
                int num3 = reader.ReadInt32();
                byte num4 = reader.ReadByte();

                int count2 = reader.ReadInt32();
                for (int index = 0; index < count2; ++index)
                {
                    string readString4;
                    ReadUE4String(reader, out readString4);
                    string readString5;
                    ReadUE4String(reader, out readString5);
                    metaInformation.Add(readString4, readString5);
                }
            }
        }

        private static void ReadUE4String(BinaryReader reader, out string readString)
        {
            readString = string.Empty;
            int count = reader.ReadInt32();
            bool flag = false;
            if (count < 0)
            {
                flag = true;
                count = -count;
            }
            if (flag || count <= 0)
                return;
            byte[] bytes = reader.ReadBytes(count);
            readString = Encoding.UTF8.GetString(bytes, 0, bytes.Length - 1);
        }

        private static void UE4ChunkUnzip(string source, string destination)
        {
            using (BinaryReader inReader = new BinaryReader(File.Open(source, FileMode.Open)))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(File.Open(destination, FileMode.Create)))
                {
                    FCompressedChunkInfo fcompressedChunkInfo1 = new FCompressedChunkInfo();
                    fcompressedChunkInfo1.Serialize(inReader);
                    FCompressedChunkInfo fcompressedChunkInfo2 = new FCompressedChunkInfo();
                    fcompressedChunkInfo2.Serialize(inReader);

                    long num1 = fcompressedChunkInfo1.CompressedSize;
                    long num2 = fcompressedChunkInfo1.UncompressedSize;
                    if (num2 == 2653586369L)
                        num2 = 131072L;
                    long length = (fcompressedChunkInfo2.UncompressedSize + num2 - 1L) / num2;

                    FCompressedChunkInfo[] fcompressedChunkInfoArray = new FCompressedChunkInfo[length];
                    long val2 = 0L;

                    for (int index = 0; index < length; ++index)
                    {
                        fcompressedChunkInfoArray[index] = new FCompressedChunkInfo();
                        fcompressedChunkInfoArray[index].Serialize(inReader);
                        val2 = Math.Max(fcompressedChunkInfoArray[index].CompressedSize, val2);
                    }

                    for (long index = 0L; index < length; ++index)
                    {
                        FCompressedChunkInfo fcompressedChunkInfo3 = fcompressedChunkInfoArray[index];
                        byte[] buffer = ZlibStream.UncompressBuffer(inReader.ReadBytes((int)fcompressedChunkInfo3.CompressedSize));
                        binaryWriter.Write(buffer);
                    }
                }
            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime datetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return datetime.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        public static List<string> ValidateModList(List<string> modIdList)
        {
            if (modIdList == null || modIdList.Count == 0)
                return new List<string>();

            // remove all null, empty, duplicate and Official mod ids.
            var newModIdList = modIdList.Where(m => !string.IsNullOrWhiteSpace(m) && !IsOfficialMod(m)).Distinct();

            return newModIdList.ToList();
        }

        public static void WriteModFile(string fileName, string modId, Dictionary<string, string> metaInformation, List<string> mapNames)
        {
            using (BinaryWriter outWriter = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                ulong num1 = ulong.Parse(modId);
                outWriter.Write(num1);
                WriteUE4String("ModName", outWriter);
                WriteUE4String(string.Empty, outWriter);
                int count1 = mapNames.Count;
                outWriter.Write(count1);
                for (int index = 0; index < mapNames.Count; ++index)
                {
                    WriteUE4String(mapNames[index], outWriter);
                }
                uint num2 = 4280483635U;
                outWriter.Write(num2);
                int num3 = 2;
                outWriter.Write(num3);
                byte num4 = metaInformation.ContainsKey("ModType") ? (byte)1 : (byte)0;
                outWriter.Write(num4);
                int count2 = metaInformation.Count;
                outWriter.Write(count2);
                foreach (KeyValuePair<string, string> keyValuePair in metaInformation)
                {
                    WriteUE4String(keyValuePair.Key, outWriter);
                    WriteUE4String(keyValuePair.Value, outWriter);
                }
            }
        }

        private static void WriteUE4String(string writeString, BinaryWriter writer)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(writeString);
            int num1 = bytes.Length + 1;
            writer.Write(num1);
            writer.Write(bytes);
            byte num2 = 0;
            writer.Write(num2);
        }
    }
}
