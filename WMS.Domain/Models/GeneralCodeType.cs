using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_GeneralCodeType")]
    public class GeneralCodeType : TenantEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!;
        [MaxLength(255)]
        public string? Description { get; set; }
    }
}
