using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_UserClaim")]
    public class UserClaim : BaseEntity
    {
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string ClaimType { get; set; } = default!;

        [MaxLength(256)]
        public string? ClaimValue { get; set; }
    }
}