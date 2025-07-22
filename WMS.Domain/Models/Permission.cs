using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_Permission")]
    public class Permission : BaseEntity
    {
        public virtual ICollection<RolePermission>? RolePermissions { get; set; }
        public virtual ICollection<UserPermission>? UserPermissions { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!;
        [MaxLength(500)]
        public string? Description { get; set; }
        [Required]
        [MaxLength(100)]
        public string Module { get; set; } = default!;
    }
}
