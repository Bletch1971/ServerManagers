using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ServerManagerTool.Common.Model
{
    public class WorkshopFileList : ObservableCollection<WorkshopFileItem>
    {
        public DateTime CachedTime
        {
            get;
            set;
        }

        public string CachedTimeFormatted
        {
            get
            {
                if (CachedTime == DateTime.MinValue)
                    return "";
                return CachedTime.ToString("G");
            }
        }

        public new void Add(WorkshopFileItem item)
        {
            if (item == null || this.Any(m => m.WorkshopId.Equals(item.WorkshopId)))
                return;

            base.Add(item);
        }

        public static WorkshopFileList GetList(WorkshopFileDetailResponse response)
        {
            var result = new WorkshopFileList();
            if (response != null)
            {
                result.CachedTime = response.cached.ToLocalTime();
                if (response.publishedfiledetails != null)
                {
                    foreach (var detail in response.publishedfiledetails)
                    {
                        result.Add(WorkshopFileItem.GetItem(detail));
                    }
                }
            }
            return result;
        }

        public override string ToString()
        {
            return $"{nameof(WorkshopFileList)} - {Count}";
        }
    }
}
