namespace WMS.Domain.DTOs.Locations
{
    public class LocationImportResult
    {
        public bool Success { get; set; }
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<LocationImportResultItem> Results { get; set; } = new();
    }
}
