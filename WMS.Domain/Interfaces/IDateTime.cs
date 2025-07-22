namespace WMS.Domain.Interfaces
{
    public interface IDateTime
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
        DateTimeOffset NowOffset { get; }
        DateTimeOffset SingaporeNow { get; }
        DateTime ToSingaporeTime(DateTime utcDateTime);
        DateTime ToUtc(DateTime singaporeDateTime);
    }
}
