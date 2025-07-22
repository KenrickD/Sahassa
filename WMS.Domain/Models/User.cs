using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_User")]
    public class User : TenantEntity
    {
        public Guid? ClientId { get; set; }
        public virtual Client? Client { get; set; }
        public virtual ICollection<UserRole>? UserRoles { get; set; }
        public virtual ICollection<UserPermission>? UserPermissions { get; set; }
        public virtual ICollection<RefreshToken>? RefreshTokens { get; set; }
        public virtual ICollection<UserClaim>? UserClaims { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = default!;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = default!;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = default!;

        [MaxLength(100)]
        public string? LastName { get; set; }

        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = default!;

        [MaxLength(256)]
        public string? SecurityStamp { get; set; }

        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastLoginDate { get; set; }
        [MaxLength(255)]
        public string? ProfileImagePath { get; set; }
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
