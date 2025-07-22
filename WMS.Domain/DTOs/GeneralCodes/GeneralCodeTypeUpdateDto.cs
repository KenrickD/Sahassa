using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.GeneralCodes
{
    public class GeneralCodeTypeUpdateDto
    {
        [Required(ErrorMessage = "Code type name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }
    }
}
