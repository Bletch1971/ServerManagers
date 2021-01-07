using System;
using System.Collections.Generic;

namespace ServerManagerTool.Common.Model
{
    public class VersionFeed
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;
        public Uri Link { get; set; } = null;
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.Now;

        public List<VersionFeedEntry> Entries { get; set; } = new List<VersionFeedEntry>();
    }
}
