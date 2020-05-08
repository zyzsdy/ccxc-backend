using System;

namespace Ccxc.Core.Utils
{
    public class UnixTimestamp
    {
        private static readonly DateTime startTime = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local);
        public static long GetTimestamp(DateTime d)
        {

            return (long)(d - startTime).TotalMilliseconds;
        }
        public static DateTime FromTimestamp(long timestamp)
        {
            return startTime.AddMilliseconds(timestamp);
        }
        public static long GetTimestampSecond(DateTime d)
        {
            return (long)(d - startTime).TotalSeconds;
        }
        public static DateTime FromTimestampSecond(long timestamp)
        {
            return startTime.AddSeconds(timestamp);
        }
    }
}
