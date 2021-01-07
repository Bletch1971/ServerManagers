using System;

namespace ServerManagerTool.Common.Utils
{
    public static class DateTimeUtils
    {
        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            TimeSpan timespan = (dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            return timespan.TotalSeconds;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime datetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return datetime.AddSeconds(unixTimeStamp).ToLocalTime();
        }
    }
}
