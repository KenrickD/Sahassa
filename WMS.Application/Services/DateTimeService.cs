using WMS.Domain.Interfaces;

namespace WMS.Application.Services
{
    public class DateTimeService : IDateTime
    {
        private static readonly TimeZoneInfo SingaporeTimeZone =
       TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"); // Windows

        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTimeOffset NowOffset => DateTimeOffset.Now;

        public DateTimeOffset SingaporeNow =>
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, SingaporeTimeZone);

        public DateTime ToSingaporeTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, SingaporeTimeZone);
        }

        public DateTime ToUtc(DateTime singaporeDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(singaporeDateTime, SingaporeTimeZone);
        }
    }
}
