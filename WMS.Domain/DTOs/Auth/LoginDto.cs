using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        public string Email { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;

        public Guid? WarehouseId { get; set; }
    }
}
