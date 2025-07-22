
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_Client")]
    public class Client : TenantEntity
    {
        public ClientConfiguration Configuration { get; set; } = null!;
        public virtual ICollection<Product> Products { get; set; } = null!;
        public virtual ICollection<Order>? Orders { get; set; }
        public virtual ICollection<Location> DedicatedLocations { get; set; } = null!;
        public virtual ICollection<User> Users { get; set; } = null!;
        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = default!;
        [MaxLength(100)]
        public string? Code { get; set; }
        [MaxLength(100)]
        public string? ContactPerson { get; set; }
        [MaxLength(100)]
        public string? ContactEmail { get; set; }
        [MaxLength(100)]
        public string? ContactPhone { get; set; }
        [MaxLength(500)]
        public string? BillingAddress { get; set; }
        [MaxLength(500)]
        public string? ShippingAddress { get; set; }
        public bool IsActive { get; set; }
    }
}
