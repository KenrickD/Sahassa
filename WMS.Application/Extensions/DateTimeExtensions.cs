namespace WMS.Application.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly TimeZoneInfo SingaporeTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        public static string ToSingaporeString(this DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var sgTime = dateTime.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(dateTime, SingaporeTimeZone)
                : dateTime;
            return sgTime.ToString(format);
        }

        public static string ToSingaporeString(this DateTimeOffset dateTimeOffset, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var sgTime = TimeZoneInfo.ConvertTime(dateTimeOffset, SingaporeTimeZone);
            return sgTime.ToString(format);
        }

        public static DateTime AsSingaporeTime(this DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return TimeZoneInfo.ConvertTimeFromUtc(dateTime, SingaporeTimeZone);
            return dateTime;
        }
    }
}
