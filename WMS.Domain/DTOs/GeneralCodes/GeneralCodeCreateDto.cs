using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.GeneralCodes
{
    public class GeneralCodeCreateDto
    {
        [Required(ErrorMessage = "Code name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Detail cannot exceed 255 characters")]
        public string? Detail { get; set; }

        public int Sequence { get; set; } = 1;

        [Required(ErrorMessage = "Code type is required")]
        public Guid GeneralCodeTypeId { get; set; }

        [Required(ErrorMessage = "Warehouse is required")]
        public Guid WarehouseId { get; set; }
    }
}
