using System;
using System.ServiceModel.Syndication;
using System.Xml;

namespace ServerManagerTool.Plugin.Discord
{
    public static class VersionFeedUtils
    {
        public static VersionFeed LoadVersionFeed(string inputUri, string currentVersion)
        {
            try
            {
                var reader = XmlReader.Create(inputUri);
                var feed = SyndicationFeed.Load(reader);

                var versionFeed = new VersionFeed
                {
                    Id = feed.Id,
                    Title = feed.Title?.Text,
                    SubTitle = feed.Description?.Text,
                    Link = feed.Links?[0].Uri,
                    Updated = feed.LastUpdatedTime.ToLocalTime(),
                };

                //Loop through all items in the SyndicationFeed
                foreach (var item in feed.Items)
                {
                    var textContent = item.Content as TextSyndicationContent;

                    var versionFeedEntry = new VersionFeedEntry
                    {
                        Id = item.Id,
                        Title = item.Title?.Text,
                        Summary = item.Summary?.Text,
                        Link = item.Links?[0].Uri,
                        Updated = item.LastUpdatedTime.ToLocalTime(),
                        Content = textContent?.Text,
                        Author = item.Authors?[0].Name,

                        IsCurrent = (item.Summary?.Text ?? string.Empty).Equals(currentVersion),
                    };
                    versionFeed.Entries.Add(versionFeedEntry);
                }

                return versionFeed;
            }
            catch (Exception)
            {
                return new VersionFeed();
            }
        }
    }
}
