using ServerManagerTool.Common.Model;
using ServerManagerTool.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ServerManagerTool.Lib
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

        public bool GetModStrings(out string mapString, out string modIdString)
        {
            mapString = null;
            modIdString = string.Empty;

            var delimiter = "";
            foreach (var mod in this)
            {
                switch (mod.ModType)
                {
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
}
