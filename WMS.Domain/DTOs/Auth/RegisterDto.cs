using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = default!;

        [Required]
        public string FirstName { get; set; } = default!;

        public string? LastName { get; set; }

        public Guid WarehouseId { get; set; }
        public Guid? ClientId { get; set; }
        public List<Guid>? RoleIds { get; set; }
    }
}