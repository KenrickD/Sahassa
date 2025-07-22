using WMS.Domain.Models;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.DTOs.Locations
{
    public class LocationImportValidationItem
    {
        public int RowNumber { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        // Location data
        public Guid WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public Guid ZoneId { get; set; }
        public string ZoneCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public LocationType Type { get; set; }
        public AccessType AccessType { get; set; }
        public string? Row { get; set; }
        public int? Bay { get; set; }
        public int? Level { get; set; }
        public string? Aisle { get; set; }
        public string? Side { get; set; }
        public string? Bin { get; set; }
        public decimal MaxWeight { get; set; }
        public decimal MaxVolume { get; set; }
        public int MaxItems { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int? PickingPriority { get; set; }
        public string? TemperatureZone { get; set; }
        public string? Barcode { get; set; }
        public decimal? XCoordinate { get; set; }
        public decimal? YCoordinate { get; set; }
        public decimal? ZCoordinate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
