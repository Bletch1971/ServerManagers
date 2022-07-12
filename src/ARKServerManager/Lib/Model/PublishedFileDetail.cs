using ServerManagerTool.Common.Model;
using ServerManagerTool.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib.Model
{
    public class ModDetailList : ObservableCollection<ModDetail>
    {
        public bool AnyUnknownModTypes
        {
            get
            {
                return this.Any(m => !m.IsValidModType);
            }
        }

        public new void Add(ModDetail mod)
        {
            if (mod == null || this.Any(m => m.ModId.Equals(mod.ModId)))
                return;

            base.Add(mod);
            SetPublishedFileIndex();
        }

        public void AddRange(ModDetail[] mods)
        {
            foreach (var mod in mods)
            {
                if (mod == null || this.Any(m => m.ModId.Equals(mod.ModId)))
                    continue;
                base.Add(mod);
            }
            SetPublishedFileIndex();
        }

        public new void Insert(int index, ModDetail mod)
        {
            if (mod == null || this.Any(m => m.ModId.Equals(mod.ModId)))
                return;

            base.Insert(index, mod);
            SetPublishedFileIndex();
        }

        public void Move(ModDetail mod, int newIndex)
        {
            if (mod == null)
                return;

            var index = base.IndexOf(mod);
            if (index <= 0)
                return;

            base.Move(index, newIndex);
            SetPublishedFileIndex();
        }

        public void MoveDown(ModDetail mod)
        {
            if (mod == null)
                return;

            var index = base.IndexOf(mod);
            if (index >= base.Count - 1)
                return;

            base.Move(index, index + 1);
            SetPublishedFileIndex();
        }

        public void MoveUp(ModDetail mod)
        {
            if (mod == null)
                return;

            var index = base.IndexOf(mod);
            if (index <= 0)
                return;

            base.Move(index, index - 1);
            SetPublishedFileIndex();
        }

        public void PopulateExtended(string modsRootFolder)
        {
            var results = new Dictionary<ModDetail, ModDetailExtended>();
            foreach (var mod in this)
            {
                results.Add(mod, new ModDetailExtended(mod.ModId));
            }

            Parallel.ForEach(results, kvp => kvp.Value.PopulateExtended(modsRootFolder));

            foreach (var kvp in results)
            {
                kvp.Key.PopulateExtended(kvp.Value);
            }
        }

        public new bool Remove(ModDetail mod)
        {
            if (mod == null)
                return false;

            var removed = base.Remove(mod);

            SetPublishedFileIndex();
            return removed;
        }

        public void SetPublishedFileIndex()
        {
            foreach (var mod in this)
            {
                mod.Index = base.IndexOf(mod) + 1;
                mod.IsFirst = false;
                mod.IsLast = false;
            }

            if (this.Count == 0)
                return;

            this[0].IsFirst = true;
            this[base.Count - 1].IsLast = true;
        }

        public bool GetModStrings(out string mapString, out string totalConversionString, out string modIdString)
        {
            mapString = null;
            totalConversionString = null;
            modIdString = string.Empty;

            var delimiter = "";
            foreach (var mod in this)
            {
                switch (mod.ModType)
                {
                    case ModUtils.MODTYPE_MAP:
                        mapString = $"/Game/Mods/{mod.ModId}/{mod.MapName}";
                        break;
                    case ModUtils.MODTYPE_TOTCONV:
                        totalConversionString = mod.ModId;
                        break;
                    case ModUtils.MODTYPE_MAPEXT:
                    case ModUtils.MODTYPE_MOD:
                    default:
                        modIdString += $"{delimiter}{mod.ModId}";
                        delimiter = ",";
                        break;
                }
            }

            return true;
        }

        public static ModDetailList GetModDetails(List<string> modIdList, string modsRootFolder, WorkshopFileList workshopFiles, PublishedFileDetailsResponse response)
        {
            var result = new ModDetailList();

            if (modIdList != null)
            {
                foreach (var modId in modIdList)
                {
                    var temp = workshopFiles?.FirstOrDefault(w => w.WorkshopId.Equals(modId));

                    result.Add(new ModDetail()
                    {
                        AppId = temp?.AppId ?? string.Empty,
                        ModId = modId,
                        TimeUpdated = -1,
                        Title = temp?.Title ?? "Mod name not available",
                        IsValid = false,
                    });
                }
            }

            if (response?.publishedfiledetails != null)
            {
                foreach (var item in result)
                {
                    var temp = response.publishedfiledetails.FirstOrDefault(w => w.publishedfileid.Equals(item.ModId));

                    if (temp != null)
                    {
                        item.AppId = temp?.creator_app_id ?? string.Empty;
                        item.ModId = temp?.publishedfileid ?? item.ModId;
                        item.TimeUpdated = temp?.time_updated ?? item.TimeUpdated;
                        item.Title = temp?.title ?? item.Title;
                        item.IsValid = temp?.creator_app_id != null;
                    }
                }
            }

            result.SetPublishedFileIndex();
            result.PopulateExtended(modsRootFolder);
            return result;
        }

        public override string ToString()
        {
            return $"{nameof(ModDetailList)} - {Count}";
        }
    }

    public class ModDetail : DependencyObject
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty AppIdProperty = DependencyProperty.Register(nameof(AppId), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(ModDetail), new PropertyMetadata(0));
        public static readonly DependencyProperty IsFirstProperty = DependencyProperty.Register(nameof(IsFirst), typeof(bool), typeof(ModDetail), new PropertyMetadata(false));
        public static readonly DependencyProperty IsLastProperty = DependencyProperty.Register(nameof(IsLast), typeof(bool), typeof(ModDetail), new PropertyMetadata(false));
        public static readonly DependencyProperty LastWriteTimeProperty = DependencyProperty.Register(nameof(LastWriteTime), typeof(DateTime), typeof(ModDetail), new PropertyMetadata(DateTime.MinValue));
        public static readonly DependencyProperty LastTimeUpdatedProperty = DependencyProperty.Register(nameof(LastTimeUpdated), typeof(int), typeof(ModDetail), new PropertyMetadata(0));
        public static readonly DependencyProperty ModIdProperty = DependencyProperty.Register(nameof(ModId), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ModTypeProperty = DependencyProperty.Register(nameof(ModType), typeof(string), typeof(ModDetail), new PropertyMetadata(ModUtils.MODTYPE_UNKNOWN));
        public static readonly DependencyProperty ModTypeStringProperty = DependencyProperty.Register(nameof(ModTypeString), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ModUrlProperty = DependencyProperty.Register(nameof(ModUrl), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty TimeUpdatedProperty = DependencyProperty.Register(nameof(TimeUpdated), typeof(int), typeof(ModDetail), new PropertyMetadata(0));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ModDetail), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty IsValidProperty = DependencyProperty.Register(nameof(IsValid), typeof(bool), typeof(ModDetail), new PropertyMetadata(false));


        public string AppId
        {
            get { return (string)GetValue(AppIdProperty); }
            set { SetValue(AppIdProperty, value); }
        }

        public int Index
        {
            get { return (int)GetValue(IndexProperty); }
            set { SetValue(IndexProperty, value); }
        }

        public bool IsFirst
        {
            get { return (bool)GetValue(IsFirstProperty); }
            set { SetValue(IsFirstProperty, value); }
        }

        public bool IsLast
        {
            get { return (bool)GetValue(IsLastProperty); }
            set { SetValue(IsLastProperty, value); }
        }

        public DateTime LastWriteTime
        {
            get { return (DateTime)GetValue(LastWriteTimeProperty); }
            set { SetValue(LastWriteTimeProperty, value); }
        }

        public int LastTimeUpdated
        {
            get { return (int)GetValue(LastTimeUpdatedProperty); }
            set { SetValue(LastTimeUpdatedProperty, value); }
        }

        public string ModId
        {
            get { return (string)GetValue(ModIdProperty); }
            set { SetValue(ModIdProperty, value); }
        }

        public string ModType
        {
            get { return (string)GetValue(ModTypeProperty); }
            set
            {
                SetValue(ModTypeProperty, value);
                SetModTypeString();
            }
        }

        public string ModTypeString
        {
            get { return (string)GetValue(ModTypeStringProperty); }
            set { SetValue(ModTypeStringProperty, value); }
        }

        public int TimeUpdated
        {
            get { return (int)GetValue(TimeUpdatedProperty); }
            set { SetValue(TimeUpdatedProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set
            {
                SetValue(TitleProperty, value);

                TitleFilterString = value?.ToLower();
            }
        }

        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            set { SetValue(IsValidProperty, value); }
        }


        public bool IsOfficialMod => ModUtils.IsOfficialMod(ModId);

        public bool IsValidModType => !string.IsNullOrWhiteSpace(ModType) && (ModType.Equals(ModUtils.MODTYPE_MAP) || ModType.Equals(ModUtils.MODTYPE_MAPEXT) || ModType.Equals(ModUtils.MODTYPE_MOD) || ModType.Equals(ModUtils.MODTYPE_TOTCONV));

        public string LastWriteTimeString => LastWriteTime == DateTime.MinValue ? string.Empty : LastWriteTime.ToString();

        public string LastWriteTimeSortString => LastWriteTime == DateTime.MinValue ? string.Empty : LastWriteTime.ToString("yyyyMMdd_HHmmss");

        public string MapName { get; set; }

        public string ModUrl => $"https://steamcommunity.com/sharedfiles/filedetails/?id={ModId}";

        public string TimeUpdatedString => TimeUpdated <= 0 ? string.Empty : ModUtils.UnixTimeStampToDateTime(TimeUpdated).ToString();

        public string TimeUpdatedSortString => TimeUpdated <= 0 ? string.Empty : ModUtils.UnixTimeStampToDateTime(TimeUpdated).ToString("yyyyMMdd_HHmmss");

        public string TitleFilterString
        {
            get;
            private set;
        }

        public bool UpToDate => !IsValid && TimeUpdated == -1 || LastTimeUpdated > 0 && LastTimeUpdated == TimeUpdated;

        public long FolderSize { get; set; }

        public string FolderSizeString
        {
            get
            {
                // GB
                var divisor = Math.Pow(1024, 3);
                if (FolderSize > divisor)
                    return $"{FolderSize / divisor:N2} GB";

                // MB
                divisor = Math.Pow(1024, 2);
                if (FolderSize > divisor)
                    return $"{FolderSize / divisor:N2} MB";

                // KB
                divisor = Math.Pow(1024, 1);
                if (FolderSize > divisor)
                    return $"{FolderSize / divisor:N2} KB";

                return $"{FolderSize} B";
            }
        }

        public void PopulateExtended(string modsRootFolder)
        {
            var modExtended = new ModDetailExtended(ModId);
            modExtended.PopulateExtended(modsRootFolder);
            PopulateExtended(modExtended);
        }

        public void PopulateExtended(ModDetailExtended extended)
        {
            LastTimeUpdated = extended.LastTimeUpdated;
            LastWriteTime = extended.LastWriteTime;
            MapName = extended.MapName;
            ModType = extended.ModType;
            FolderSize = extended.FolderSize;
        }

        public void SetModTypeString()
        {
            if (string.IsNullOrWhiteSpace(ModType))
                ModTypeString = _globalizer.GetResourceString("ModType_Unknown");

            switch (ModType)
            {
                case ModUtils.MODTYPE_MAP:
                    ModTypeString = _globalizer.GetResourceString("ModType_Map");
                    break;
                case ModUtils.MODTYPE_MAPEXT:
                    ModTypeString = _globalizer.GetResourceString("ModType_MapExtension");
                    break;
                case ModUtils.MODTYPE_MOD:
                    ModTypeString = _globalizer.GetResourceString("ModType_Mod");
                    break;
                case ModUtils.MODTYPE_TOTCONV:
                    ModTypeString = _globalizer.GetResourceString("ModType_TotalConversion");
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(AppId))
                        ModTypeString = _globalizer.GetResourceString("ModType_Unknown");
                    else
                        ModTypeString = _globalizer.GetResourceString("ModType_NotDownloaded");
                    break;
            }
        }


        public static ModDetail GetModDetail(PublishedFileDetail detail)
        {
            var result = new ModDetail();
            result.AppId = detail.creator_app_id;
            result.ModId = detail.publishedfileid;
            result.TimeUpdated = detail.time_updated;
            result.Title = detail.title;
            result.IsValid = true;
            return result;
        }

        public static ModDetail GetModDetail(WorkshopFileDetail detail)
        {
            var result = new ModDetail();
            result.AppId = detail.creator_appid;
            result.ModId = detail.publishedfileid;
            result.TimeUpdated = detail.time_updated;
            result.Title = detail.title;
            result.IsValid = true;
            return result;
        }

        public static ModDetail GetModDetail(WorkshopFileItem detail)
        {
            var result = new ModDetail();
            result.AppId = detail.AppId;
            result.ModId = detail.WorkshopId;
            result.TimeUpdated = detail.TimeUpdated;
            result.Title = detail.Title;
            result.IsValid = true;
            return result;
        }

        public override string ToString()
        {
            return $"{ModId} - {Title}";
        }
    }

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
                var modFolder = Path.Combine(modsRootFolder, ModId);
                var modFile = $"{modFolder}.mod";

                if (string.IsNullOrWhiteSpace(modFolder) || !Directory.Exists(modFolder))
                    return;
                if (string.IsNullOrWhiteSpace(modFile) || !File.Exists(modFile))
                    return;

                LastWriteTime = File.GetLastWriteTime(modFile);

                var modTimeFile = Path.Combine(modFolder, Config.Default.LastUpdatedTimeFile);
                if (!string.IsNullOrWhiteSpace(modTimeFile) && File.Exists(modTimeFile))
                {
                    LastTimeUpdated = ModUtils.GetModLatestTime(modTimeFile);
                }

                ModUtils.ReadModFile(modFile, out string modId, out Dictionary<string, string> metaInformation, out List<string> mapNames);

                ModType = metaInformation != null && metaInformation.ContainsKey("ModType") ? metaInformation["ModType"] : ModUtils.MODTYPE_UNKNOWN;
                MapName = mapNames != null && mapNames.Count > 0 ? mapNames[0] : string.Empty;

                FolderSize = 0;
                foreach (var file in new DirectoryInfo(modFolder).GetFiles("*.*", SearchOption.AllDirectories))
                {
                    FolderSize += file.Length;
                }
            }
            catch (Exception)
            {
                // do nothing
            }
        }
    }
}
