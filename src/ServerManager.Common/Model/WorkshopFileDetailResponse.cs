using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace ServerManagerTool.Common.Model
{
    public class WorkshopFileDetailResponse
    {
        public DateTime cached = DateTime.UtcNow;

        public int total { get; set; }

        public List<WorkshopFileDetail> publishedfiledetails { get; set; }

        public static WorkshopFileDetailResponse Load(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            return JsonUtils.DeserializeFromFile<WorkshopFileDetailResponse>(file);
        }

        public bool Save(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return false;

            return JsonUtils.SerializeToFile(this, file);
        }
    }
}
