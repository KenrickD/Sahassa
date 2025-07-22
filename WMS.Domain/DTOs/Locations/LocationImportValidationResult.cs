namespace WMS.Domain.DTOs.Locations
{
    public class LocationImportValidationResult
    {
        public bool IsValid { get; set; }
        public int TotalRows { get; set; }
        public List<LocationImportValidationItem> ValidItems { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
