using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.Roles
{
    public class RoleViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Role name is required")]
        [StringLength(100, ErrorMessage = "Role name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; } = true;

        // Read-only properties for display
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties count for display
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }

        public ICollection<RolePermissionViewModel>? RolePermissions { get; set; }
    }
}
