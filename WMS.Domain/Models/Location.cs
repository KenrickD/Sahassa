
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.Models
{
    [Table("TB_Location")]
    public class Location : TenantEntity
    {
        public virtual ICollection<Inventory> Inventories { get; set; } = null!;
        public virtual ICollection<GIV_RM_ReceivePallet> GIVRMReceivePallets { get; set; } = null!;
        public virtual ICollection<GIV_FG_ReceivePallet> GIVFGReceivePallets { get; set; } = null!;
        public Guid ZoneId { get; set; }
        public virtual Zone Zone { get; set; } = null!;

        public LocationType Type { get; set; } // Keep this for different location types

        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = default!; // e.g., "A0101" or "Rack A Bay 01 Level 01"

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = default!; // e.g., "A0101"

        public bool IsActive { get; set; }
        public bool IsEmpty { get; set; }

        // Capacity constraints
        public decimal MaxWeight { get; set; }
        public decimal MaxVolume { get; set; }
        public int MaxItems { get; set; }

        [MaxLength(500)]
        public string? Barcode { get; set; }

        // Physical dimensions
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }

        // Positioning coordinates for racking systems
        [MaxLength(10)]
        public string? Row { get; set; }  // Rack section (Alphabet)

        public int? Bay { get; set; }

        public int? Level { get; set; } // Vertical level (1-5 in your image)

        // Additional positioning fields that might be useful
        [MaxLength(10)]
        public string? Aisle { get; set; }   // If you have multiple aisles (A01, A02, etc.)

        [MaxLength(10)]
        public string? Side { get; set; }    // Left/Right side of aisle ("L", "R")

        // For bin locations within a rack position
        [MaxLength(10)]
        public string? Bin { get; set; }     // Sub-location within the rack position

        // Position priority for picking optimization
        public int? PickingPriority { get; set; }

        // Temperature zone (if applicable)
        [MaxLength(50)]
        public string? TemperatureZone { get; set; } // "Ambient", "Chilled", "Frozen"

        // Access type
        public AccessType AccessType { get; set; }

        // Calculated fields for easier querying
        [MaxLength(50)]
        public string? FullLocationCode { get; set; } // Auto-generated: "A01-01-01" (Bay-Row-Level)

        // Coordinate system (X, Y, Z for 3D positioning)
        public decimal? XCoordinate { get; set; }
        public decimal? YCoordinate { get; set; }
        public decimal? ZCoordinate { get; set; }
    }
}
