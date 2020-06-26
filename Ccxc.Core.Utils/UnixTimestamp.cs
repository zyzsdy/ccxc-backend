using System;

namespace Ccxc.Core.Utils
{
    public class UnixTimestamp
    {
        private static readonly DateTime StartTime = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local);
        public static long GetTimestamp(DateTime d)
        {

            return (long)(d - StartTime).TotalMilliseconds;
        }
        public static DateTime FromTimestamp(long timestamp)
        {
            return StartTime.AddMilliseconds(timestamp);
        }
        public static long GetTimestampSecond(DateTime d)
        {
            return (long)(d - StartTime).TotalSeconds;
        }
        public static DateTime FromTimestampSecond(long timestamp)
        {
            return StartTime.AddSeconds(timestamp);
        }
    }
}
