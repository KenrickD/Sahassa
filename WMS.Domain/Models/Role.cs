using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_Role")]
    public class Role : BaseEntity
    {
        public virtual ICollection<UserRole>? UserRoles { get; set; }
        public virtual ICollection<RolePermission>? RolePermissions { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!;
        [MaxLength(500)]
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; } // True for built-in roles      
    }
}
