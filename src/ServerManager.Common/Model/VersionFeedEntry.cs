using System;

namespace ServerManagerTool.Common.Model
{
    public class VersionFeedEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public Uri Link { get; set; } = null;
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.Now;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;

        public bool IsCurrent { get; set; } = false;
    }
}
