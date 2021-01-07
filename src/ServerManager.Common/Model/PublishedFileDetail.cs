using System.Collections.Generic;

namespace ServerManagerTool.Common.Model
{
    public class PublishedFileDetail
    {
        public string publishedfileid { get; set; }

        public int result { get; set; }

        public string creator { get; set; }

        public string creator_app_id { get; set; }

        public string consumer_app_id { get; set; }

        public string filename { get; set; }

        public string file_size { get; set; }

        public string file_url { get; set; }

        public string hcontent_file { get; set; }

        public string preview_url { get; set; }

        public string hcontent_preview { get; set; }

        public string title { get; set; }

        public string description { get; set; }

        public int time_created { get; set; }

        public int time_updated { get; set; }

        public int visibility { get; set; }

        public int banned { get; set; }

        public string ban_reason { get; set; }

        public int subscriptions { get; set; }

        public int favorited { get; set; }

        public int lifetime_subscriptions { get; set; }

        public int lifetime_favorited { get; set; }

        public int views { get; set; }

        public List<object> tags { get; set; }
    }
}
