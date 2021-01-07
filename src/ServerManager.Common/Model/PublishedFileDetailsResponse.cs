using System.Collections.Generic;

namespace ServerManagerTool.Common.Model
{
    public class PublishedFileDetailsResponse
    {
        public int result { get; set; }

        public int resultcount { get; set; }

        public List<PublishedFileDetail> publishedfiledetails { get; set; }
    }
}
