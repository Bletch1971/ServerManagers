using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Utils;
using System;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib
{
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


        public bool IsValidModType => !string.IsNullOrWhiteSpace(ModType) && (ModType.Equals(ModUtils.MODTYPE_MAP) || ModType.Equals(ModUtils.MODTYPE_MOD));

        public string LastWriteTimeString => LastWriteTime == DateTime.MinValue ? string.Empty : LastWriteTime.ToString();

        public string LastWriteTimeSortString => LastWriteTime == DateTime.MinValue ? string.Empty : LastWriteTime.ToString("yyyyMMdd_HHmmss");

        public string MapName { get; set; }

        public string ModUrl => $"https://steamcommunity.com/sharedfiles/filedetails/?id={ModId}";

        public string TimeUpdatedString => TimeUpdated <= 0 ? string.Empty : DateTimeUtils.UnixTimeStampToDateTime(TimeUpdated).ToString();

        public string TimeUpdatedSortString => TimeUpdated <= 0 ? string.Empty : DateTimeUtils.UnixTimeStampToDateTime(TimeUpdated).ToString("yyyyMMdd_HHmmss");

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

                case ModUtils.MODTYPE_MOD:
                    ModTypeString = _globalizer.GetResourceString("ModType_Mod");
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
            var result = new ModDetail()
            {
                AppId = detail.creator_app_id,
                ModId = detail.publishedfileid,
                TimeUpdated = detail.time_updated,
                Title = detail.title,
                IsValid = true,
            };
            return result;
        }

        public static ModDetail GetModDetail(WorkshopFileDetail detail)
        {
            var result = new ModDetail()
            {
                AppId = detail.creator_appid,
                ModId = detail.publishedfileid,
                TimeUpdated = detail.time_updated,
                Title = detail.title,
                IsValid = true,
            };
            return result;
        }

        public static ModDetail GetModDetail(WorkshopFileItem detail)
        {
            var result = new ModDetail()
            {
                AppId = detail.AppId,
                ModId = detail.WorkshopId,
                TimeUpdated = detail.TimeUpdated,
                Title = detail.Title,
                IsValid = true,
            };
            return result;
        }

        public override string ToString()
        {
            return $"{ModId} - {Title}";
        }
    }
}
