using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Users
{
    public class UserCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? LastName { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public Guid? ClientId { get; set; }

        [Required]
        public Guid WarehouseId { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
        public IFormFile? ProfileImage { get; set; }
    }
}
