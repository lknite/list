using System.Reflection.Metadata.Ecma335;

namespace list.Helpers
{
    public static class Timestamp
    {
        public static long getUtcTimestampInMilliseconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        public static DateTime getUtcDateTimeFromTimestampInMilliseconds(long milliseconds)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds( milliseconds ).UtcDateTime;
        }
        public static long getTimestampInMilliseconds()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
        public static DateTime getDateTimeFromTimestampInMilliseconds(long milliseconds)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds( milliseconds ).DateTime;
        }
    }
}
