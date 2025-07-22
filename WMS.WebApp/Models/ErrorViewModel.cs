namespace WMS.WebApp.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? CorrelationId { get; set; }
        public string? ErrorMessage { get; set; }
        public int? StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string? RequestPath { get; set; }
        public string? UserAgent { get; set; }

        // Helper properties
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public bool ShowCorrelationId => !string.IsNullOrEmpty(CorrelationId);
        public string FormattedTimestamp => TimeZoneInfo
    .ConvertTimeFromUtc(Timestamp, TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"))
    .ToString("yyyy-MM-dd HH:mm:ss 'SGT'");
    }
}
