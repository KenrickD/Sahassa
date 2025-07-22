using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_GeneralCode")]
    public class GeneralCode : TenantEntity
    {
        public Guid GeneralCodeTypeId { get; set; }
        public virtual GeneralCodeType GeneralCodeType { get; set; } = null!;
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!;
        [MaxLength(255)]
        public string? Detail { get; set; }
        public int Sequence { get; set; }
    }
}
