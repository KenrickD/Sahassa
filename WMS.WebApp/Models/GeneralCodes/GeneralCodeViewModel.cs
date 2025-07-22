using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.GeneralCodes
{
    public class GeneralCodeViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Code name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Detail cannot exceed 255 characters")]
        public string? Detail { get; set; }

        public int Sequence { get; set; } = 1;

        [Required(ErrorMessage = "Code type is required")]
        public Guid GeneralCodeTypeId { get; set; }
        public string GeneralCodeTypeName { get; set; } = string.Empty;

        // Display properties
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
