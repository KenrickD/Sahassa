using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.GeneralCodes
{
    public class GeneralCodeTypeViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Code type name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Warehouse is required")]
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public int CodesCount { get; set; }

        // Display properties
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
