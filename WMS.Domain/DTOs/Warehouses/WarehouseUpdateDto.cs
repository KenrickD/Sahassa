using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Warehouses
{
    public class WarehouseUpdateDto
    {
        [Required(ErrorMessage = "Warehouse name is required")]
        [MaxLength(250, ErrorMessage = "Name cannot exceed 250 characters")]
        public string Name { get; set; } = default!;

        [Required(ErrorMessage = "Warehouse code is required")]
        [MaxLength(100, ErrorMessage = "Code cannot exceed 100 characters")]
        public string Code { get; set; } = default!;

        [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string? City { get; set; }

        [MaxLength(100, ErrorMessage = "State cannot exceed 100 characters")]
        public string? State { get; set; }

        [MaxLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string? Country { get; set; }

        [MaxLength(20, ErrorMessage = "Zip code cannot exceed 20 characters")]
        public string? ZipCode { get; set; }

        [MaxLength(100, ErrorMessage = "Contact person cannot exceed 100 characters")]
        public string? ContactPerson { get; set; }

        [MaxLength(100, ErrorMessage = "Contact email cannot exceed 100 characters")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? ContactEmail { get; set; }

        [MaxLength(100, ErrorMessage = "Contact phone cannot exceed 100 characters")]
        public string? ContactPhone { get; set; }

        public bool IsActive { get; set; }
    }
}