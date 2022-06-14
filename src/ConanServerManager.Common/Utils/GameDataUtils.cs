using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace ServerManagerTool.Utils
{
    public static class GameDataUtils
    {
        public static void ReadAllData(out MainGameData data, string dataFolder, string extension, string application, bool isUserData = false)
        {
            data = new MainGameData();

            if (string.IsNullOrWhiteSpace(dataFolder))
                return;

            if (!Directory.Exists(dataFolder))
                return;

            foreach (var file in Directory.GetFiles(dataFolder, $"*{extension}", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var fileData = MainGameData.Load(file, isUserData);
                    if (fileData == null)
                        continue;

                    if (!fileData.Application.Equals(application, StringComparison.OrdinalIgnoreCase))
                        continue;

                    data.GameMaps.AddRange(fileData.GameMaps);
                    data.Branches.AddRange(fileData.Branches);
                    data.ServerRegions.AddRange(fileData.ServerRegions);
                    data.RconInputModes.AddRange(fileData.RconInputModes);
                }
                catch
                {
                    // do nothing, just swallow the error
                }
            }
        }
    }

    [DataContract]
    public class BaseGameData
    {
        public string GameDataFile = string.Empty;

        [DataMember]
        public string Application = string.Empty;
        [DataMember]
        public string Version = "1.0.0";
        [DataMember]
        public DateTime Created = DateTime.UtcNow;
        [DataMember]
        public string Color = "White";

        public static BaseGameData Load(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            var data = JsonUtils.DeserializeFromFile<BaseGameData>(file);
            if (data != null)
            {
                data.GameDataFile = file;
            }
            return data;
        }

        public bool Save(string file)
        {
            var folder = Path.GetDirectoryName(file);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return JsonUtils.SerializeToFile(this, file);
        }
    }

    [DataContract]
    public class MainGameData : BaseGameData
    {
        [DataMember(IsRequired = false)]
        public List<GameMapDataItem> GameMaps = new List<GameMapDataItem>();

        [DataMember(IsRequired = false)]
        public List<BranchDataItem> Branches = new List<BranchDataItem>();

        [DataMember(IsRequired = false)]
        public List<ServerRegionDataItem> ServerRegions = new List<ServerRegionDataItem>();

        [DataMember(IsRequired = false)]
        public List<RconInputModeItem> RconInputModes = new List<RconInputModeItem>();

        public static MainGameData Load(string file, bool isUserData)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            var data = JsonUtils.DeserializeFromFile<MainGameData>(file);
            if (data != null)
            {
                data.GameDataFile = file;
                data.GameMaps.ForEach(c => c.IsUserData = isUserData);
                data.Branches.ForEach(c => c.IsUserData = isUserData);
                data.ServerRegions.ForEach(c => c.IsUserData = isUserData);
                data.RconInputModes.ForEach(c => c.IsUserData = isUserData);
            }
            return data;
        }
    }

    [DataContract]
    public class BaseDataItem
    {
        [DataMember]
        public string ClassName = string.Empty;
        [DataMember]
        public string Description = string.Empty;
        [DataMember]
        public string Mod = string.Empty;

        public bool IsUserData = false;
    }

    [DataContract]
    public class GameMapDataItem : BaseDataItem
    {
        [DataMember]
        public string SaveFileName = string.Empty;
    }

    [DataContract]
    public class BranchDataItem
    {
        [DataMember]
        public string BranchName = string.Empty;
        [DataMember]
        public string Description = string.Empty;

        public bool IsUserData = false;
    }

    [DataContract]
    public class ServerRegionDataItem
    {
        [DataMember]
        public string RegionNumber = string.Empty;
        [DataMember]
        public string Description = string.Empty;

        public bool IsUserData = false;
    }

    [DataContract]
    public class RconInputModeItem
    {
        [DataMember]
        public string Command = string.Empty;
        [DataMember]
        public string Description = string.Empty;

        public bool IsUserData = false;
    }
}
