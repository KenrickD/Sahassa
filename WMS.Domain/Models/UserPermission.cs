using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_UserPermission")]
    public class UserPermission : BaseEntity
    {
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public Guid PermissionId { get; set; }
        public virtual Permission Permission { get; set; } = null!;
    }
}
