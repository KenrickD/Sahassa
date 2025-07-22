using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_Zone")]
    public class Zone : TenantEntity
    {
        public virtual ICollection<Location> Locations { get; set; } = null!;
        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = default!;
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = default!;
        //public ZoneType Type { get; set; }
        public bool IsActive { get; set; }
        [MaxLength(250)]
        public string? Description { get; set; }

    }
    public enum ZoneType
    {
        Receiving,
        Storage,
        Picking,
        Packing,
        Shipping,
        QualityControl,
        Returns,
        Damaged
    }

}
