using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.Users
{
    public class UserUpdateDto
    {
        [Required]
        public Guid Id { get; set; }

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

        public bool IsActive { get; set; }

        [Required]
        public List<Guid> RoleIds { get; set; } = new List<Guid>();

        public bool RemoveProfileImage { get; set; }
        public IFormFile? ProfileImage { get; set; }
        public bool HasEditAccess { get; set; }
        public bool IsOwnProfile { get; set; }
    }
}
