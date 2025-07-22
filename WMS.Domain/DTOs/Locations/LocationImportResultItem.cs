namespace WMS.Domain.DTOs.Locations
{
    public class LocationImportResultItem
    {
        public int RowNumber { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Success, Error, Warning
        public string Message { get; set; } = string.Empty;
    }
}
