using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Zones
{
    public class ZoneCreateDto
    {
        [Required(ErrorMessage = "Zone name is required")]
        [StringLength(250, ErrorMessage = "Zone name cannot exceed 250 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Zone code is required")]
        [StringLength(100, ErrorMessage = "Zone code cannot exceed 100 characters")]
        public string Code { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Warehouse is required")]
        public Guid WarehouseId { get; set; }
    }
}
