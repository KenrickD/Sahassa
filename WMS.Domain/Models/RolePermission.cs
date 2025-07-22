using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_RolePermission")]
    public class RolePermission : BaseEntity
    {
        public Guid RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;
        public Guid PermissionId { get; set; }
        public virtual Permission Permission { get; set; } = null!;
    }
}
