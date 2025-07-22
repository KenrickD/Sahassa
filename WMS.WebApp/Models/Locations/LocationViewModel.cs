using System.ComponentModel.DataAnnotations;
using WMS.Domain.Models;
namespace WMS.WebApp.Models.Locations
{
    public class LocationViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Zone is required")]
        public Guid ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string ZoneCode { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        //public LocationType Type { get; set; } = LocationType.Floor;

        [Required(ErrorMessage = "Location name is required")]
        [StringLength(250, ErrorMessage = "Location name cannot exceed 250 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location code is required")]
        [StringLength(100, ErrorMessage = "Location code cannot exceed 100 characters")]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsEmpty { get; set; } = true;

        [Range(0, double.MaxValue, ErrorMessage = "Max weight must be a positive value")]
        public decimal MaxWeight { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Max volume must be a positive value")]
        public decimal MaxVolume { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Max items must be a positive value")]
        public int MaxItems { get; set; }

        [StringLength(500, ErrorMessage = "Barcode cannot exceed 500 characters")]
        public string? Barcode { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Length must be a positive value")]
        public decimal Length { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Width must be a positive value")]
        public decimal Width { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Height must be a positive value")]
        public decimal Height { get; set; }

        [StringLength(10, ErrorMessage = "Row cannot exceed 10 characters")]
        public string? Row { get; set; }

        [Range(1, 999, ErrorMessage = "Bay must be between 1 and 999")]
        public int? Bay { get; set; }

        [Range(1, 999, ErrorMessage = "Level must be between 1 and 999")]
        public int? Level { get; set; }

        [StringLength(10, ErrorMessage = "Aisle cannot exceed 10 characters")]
        public string? Aisle { get; set; }

        [StringLength(10, ErrorMessage = "Side cannot exceed 10 characters")]
        public string? Side { get; set; }

        [StringLength(10, ErrorMessage = "Bin cannot exceed 10 characters")]
        public string? Bin { get; set; }

        //[Range(0, int.MaxValue, ErrorMessage = "Picking priority must be a positive value")]
        //public int? PickingPriority { get; set; }

        //[StringLength(50, ErrorMessage = "Temperature zone cannot exceed 50 characters")]
        //public string? TemperatureZone { get; set; }

        //public AccessType AccessType { get; set; } = AccessType.Manual;

        public string? FullLocationCode { get; set; }

        public decimal? XCoordinate { get; set; }
        public decimal? YCoordinate { get; set; }
        public decimal? ZCoordinate { get; set; }

        // Display properties
        public int InventoryCount { get; set; }
        public decimal CurrentUtilization { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Helper methods for display
        public string GetPositionDisplay()
        {
            if (!string.IsNullOrEmpty(Row) && Bay.HasValue && Level.HasValue)
            {
                return $"{Row}-{Bay:00}-{Level:00}";
            }
            if (!string.IsNullOrEmpty(FullLocationCode))
            {
                return FullLocationCode;
            }
            return Code;
        }

        public string GetUtilizationColor()
        {
            return CurrentUtilization switch
            {
                >= 90 => "text-red-600",
                >= 70 => "text-yellow-600",
                >= 50 => "text-blue-600",
                _ => "text-green-600"
            };
        }
    }
}