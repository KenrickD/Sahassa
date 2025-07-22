
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_UserRole")]
    public class UserRole : BaseEntity
    {
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public Guid RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;
    }
}
