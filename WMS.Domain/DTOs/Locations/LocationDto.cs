using WMS.Domain.Models;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.DTOs.Locations
{
    public class LocationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsEmpty { get; set; }
        public LocationType Type { get; set; }
        public AccessType AccessType { get; set; }

        // Capacity constraints
        public decimal MaxWeight { get; set; }
        public decimal MaxVolume { get; set; }
        public int MaxItems { get; set; }
        public string? Barcode { get; set; }

        // Physical dimensions
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }

        // Positioning coordinates
        public string? Row { get; set; }
        public int? Bay { get; set; }
        public int? Level { get; set; }
        public string? Aisle { get; set; }
        public string? Side { get; set; }
        public string? Bin { get; set; }

        // Additional properties
        public int? PickingPriority { get; set; }
        public string? TemperatureZone { get; set; }
        public string? FullLocationCode { get; set; }

        // Coordinates
        public decimal? XCoordinate { get; set; }
        public decimal? YCoordinate { get; set; }
        public decimal? ZCoordinate { get; set; }

        // Navigation properties
        public Guid ZoneId { get; set; }
        public string ZoneCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;

        // Calculated properties
        public int InventoryCount { get; set; }
        public decimal CurrentUtilization { get; set; } // Percentage of capacity used

        // Audit properties
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        // this two for decide whether show and hide edit and delete button
        public bool HasWriteAccess { get; set; }
        public bool HasDeleteAccess { get; set; }
    }
}
