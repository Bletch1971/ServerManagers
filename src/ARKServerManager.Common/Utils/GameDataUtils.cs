using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
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

                    data.Creatures.AddRange(fileData.Creatures);
                    data.Engrams.AddRange(fileData.Engrams);
                    data.Items.AddRange(fileData.Items);
                    data.MapSpawners.AddRange(fileData.MapSpawners);
                    data.SupplyCrates.AddRange(fileData.SupplyCrates);
                    data.Inventories.AddRange(fileData.Inventories);
                    data.GameMaps.AddRange(fileData.GameMaps);
                    data.TotalConversions.AddRange(fileData.TotalConversions);
                    data.PlayerLevels.AddRange(fileData.PlayerLevels);
                    data.CreatureLevels.AddRange(fileData.CreatureLevels);
                    data.Branches.AddRange(fileData.Branches);
                    data.Events.AddRange(fileData.Events);
                    data.OfficialMods.AddRange(fileData.OfficialMods);
                    data.RconInputModes.AddRange(fileData.RconInputModes);

                    if (fileData.PlayerAdditionalLevels > 0 && fileData.PlayerAdditionalLevels > data.PlayerAdditionalLevels)
                        data.PlayerAdditionalLevels = fileData.PlayerAdditionalLevels;
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
        public List<CreatureDataItem> Creatures = new List<CreatureDataItem>();

        [DataMember(IsRequired = false)]
        public List<EngramDataItem> Engrams = new List<EngramDataItem>();

        [DataMember(IsRequired = false)]
        public List<ItemDataItem> Items = new List<ItemDataItem>();

        [DataMember(IsRequired = false)]
        public List<MapSpawnerDataItem> MapSpawners = new List<MapSpawnerDataItem>();

        [DataMember(IsRequired = false)]
        public List<SupplyCrateDataItem> SupplyCrates = new List<SupplyCrateDataItem>();

        [DataMember(IsRequired = false)]
        public List<InventoryDataItem> Inventories = new List<InventoryDataItem>();

        [DataMember(IsRequired = false)]
        public List<GameMapDataItem> GameMaps = new List<GameMapDataItem>();

        [DataMember(IsRequired = false)]
        public List<TotalConversionDataItem> TotalConversions = new List<TotalConversionDataItem>();

        [DataMember(IsRequired = false)]
        public List<PlayerLevelDataItem> PlayerLevels = new List<PlayerLevelDataItem>();

        [DataMember(IsRequired = false)]
        public int PlayerAdditionalLevels = 0;

        [DataMember(IsRequired = false)]
        public List<CreatureLevelDataItem> CreatureLevels = new List<CreatureLevelDataItem>();

        [DataMember(IsRequired = false)]
        public List<BranchDataItem> Branches = new List<BranchDataItem>();

        [DataMember(IsRequired = false)]
        public List<EventDataItem> Events = new List<EventDataItem>();

        [DataMember(IsRequired = false)]
        public List<OfficialModItem> OfficialMods = new List<OfficialModItem>();

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
                data.Creatures.ForEach(c => c.IsUserData = isUserData);
                data.Engrams.ForEach(c => c.IsUserData = isUserData);
                data.Items.ForEach(c => c.IsUserData = isUserData);
                data.MapSpawners.ForEach(c => c.IsUserData = isUserData);
                data.SupplyCrates.ForEach(c => c.IsUserData = isUserData);
                data.Inventories.ForEach(c => c.IsUserData = isUserData);
                data.GameMaps.ForEach(c => c.IsUserData = isUserData);
                data.TotalConversions.ForEach(c => c.IsUserData = isUserData);
                data.Branches.ForEach(c => c.IsUserData = isUserData);
                data.Events.ForEach(c => c.IsUserData = isUserData);
                data.OfficialMods.ForEach(c => c.IsUserData = isUserData);
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
    public class CreatureDataItem : BaseDataItem
    {
        [DataMember]
        public string NameTag = string.Empty;
        [DataMember]
        public bool IsSpawnable = false;
        [DataMember(Name = "IsTameable")]
        public string IsTameableString
        {
            get
            {
                return IsTameable.ToString();
            }
            set
            {
                if (!Enum.TryParse(value, true, out IsTameable))
                    IsTameable = DinoTamable.False;
            }
        }

        public DinoTamable IsTameable = DinoTamable.False;

        [DataMember(Name = "IsBreedingable")]
        public string IsBreedingableString
        {
            get
            {
                return IsBreedingable.ToString();
            }
            set
            {
                if (!Enum.TryParse(value, true, out IsBreedingable))
                    IsBreedingable = DinoBreedingable.False;
            }
        }

        public DinoBreedingable IsBreedingable = DinoBreedingable.False;
    }

    [DataContract]
    public class EngramDataItem : BaseDataItem
    {
        [DataMember]
        public int Level = 0;
        [DataMember]
        public int Points = 0;
        [DataMember]
        public bool IsTekGram = false;
    }

    [DataContract]
    public class ItemDataItem : BaseDataItem
    {
        [DataMember]
        public string Category = string.Empty;
        [DataMember]
        public bool IsHarvestable = false;
    }

    [DataContract]
    public class MapSpawnerDataItem : BaseDataItem
    {
    }

    [DataContract]
    public class SupplyCrateDataItem : BaseDataItem
    {
    }

    [DataContract]
    public class InventoryDataItem : BaseDataItem
    {
    }

    [DataContract]
    public class GameMapDataItem : BaseDataItem
    {
        [DataMember]
        public bool IsSotF = false;
    }

    [DataContract]
    public class TotalConversionDataItem : BaseDataItem
    {
        [DataMember]
        public bool IsSotF = false;
    }

    [DataContract]
    public class PlayerLevelDataItem
    {
        [DataMember]
        public long XPRequired = 0;
        [DataMember]
        public long EngramPoints = 0;
    }

    [DataContract]
    public class CreatureLevelDataItem
    {
        [DataMember]
        public long XPRequired = 0;
    }

    [DataContract]
    public class BranchDataItem
    {
        [DataMember]
        public bool IsSotF = false;
        [DataMember]
        public string BranchName = string.Empty;
        [DataMember]
        public string Description = string.Empty;

        public bool IsUserData = false;
    }

    [DataContract]
    public class EventDataItem
    {
        [DataMember]
        public bool IsSotF = false;
        [DataMember]
        public string EventName = string.Empty;
        [DataMember]
        public string Description = string.Empty;

        public bool IsUserData = false;
    }

    [DataContract]
    public class OfficialModItem
    {
        [DataMember]
        public string ModId = string.Empty;
        [DataMember]
        public string ModName = string.Empty;

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
