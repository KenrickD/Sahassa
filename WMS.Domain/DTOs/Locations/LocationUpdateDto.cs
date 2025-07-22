using System.ComponentModel.DataAnnotations;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.Locations
{
    public class LocationUpdateDto
    {
        [Required(ErrorMessage = "Location name is required")]
        [StringLength(250, ErrorMessage = "Location name cannot exceed 250 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location code is required")]
        [StringLength(100, ErrorMessage = "Location code cannot exceed 100 characters")]
        public string Code { get; set; } = string.Empty;

        //public LocationType Type { get; set; }
        //public AccessType AccessType { get; set; }
        public bool IsActive { get; set; }

        // Capacity constraints
        [Range(0, double.MaxValue, ErrorMessage = "Max weight must be a positive value")]
        public decimal MaxWeight { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Max volume must be a positive value")]
        public decimal MaxVolume { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Max items must be a positive value")]
        public int MaxItems { get; set; }

        [StringLength(500, ErrorMessage = "Barcode cannot exceed 500 characters")]
        public string? Barcode { get; set; }

        // Physical dimensions
        [Range(0, double.MaxValue, ErrorMessage = "Length must be a positive value")]
        public decimal Length { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Width must be a positive value")]
        public decimal Width { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Height must be a positive value")]
        public decimal Height { get; set; }

        // Positioning coordinates
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

        // Additional properties
        //[Range(0, int.MaxValue, ErrorMessage = "Picking priority must be a positive value")]
        //public int? PickingPriority { get; set; }

        //[StringLength(50, ErrorMessage = "Temperature zone cannot exceed 50 characters")]
        //public string? TemperatureZone { get; set; }

        // Coordinates
        public decimal? XCoordinate { get; set; }
        public decimal? YCoordinate { get; set; }
        public decimal? ZCoordinate { get; set; }
    }
}
