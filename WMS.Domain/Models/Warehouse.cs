
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_Warehouse")]
    public class Warehouse : BaseEntity
    {
        public WarehouseConfiguration Configuration { get; set; } = null!;
        public virtual ICollection<Zone> Zones { get; set; } = null!;
        public virtual ICollection<Client> Clients { get; set; } = null!;
        public virtual ICollection<User> Users { get; set; } = null!;
        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = default!;
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = default!;
        [MaxLength(500)]
        public string? Address { get; set; }
        [MaxLength(100)]
        public string? City { get; set; }
        [MaxLength(100)]
        public string? State { get; set; }
        [MaxLength(100)]
        public string? Country { get; set; }
        [MaxLength(20)]
        public string? ZipCode { get; set; }
        [MaxLength(100)]
        public string? ContactPerson { get; set; }
        [MaxLength(100)]
        public string? ContactEmail { get; set; }
        [MaxLength(100)]
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; }

    }
}
