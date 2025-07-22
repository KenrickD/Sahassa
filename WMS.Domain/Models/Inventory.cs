
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.Models
{
    [Table("TB_Inventory")]
    public class Inventory : TenantEntity
    {
        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;
        public Guid LocationId { get; set; }
        public virtual Location Location { get; set; } = null!;
        public Guid ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;
        public virtual ICollection<InventoryMovement> Movements { get; set; } = null!;
        [Required]
        [MaxLength(100)]
        public string LotNumber { get; set; } = default!;
        [Required]
        [MaxLength(100)]
        public string SerialNumber { get; set; } = default!;
        public DateTime? ExpirationDate { get; set; }
        public decimal Quantity { get; set; }
        public InventoryStatus Status { get; set; }
        public DateTime ReceivedDate { get; set; }
        [MaxLength(100)]
        public string? PONumber { get; set; }
        public decimal CostPrice { get; set; }
        public Guid? FileUploadId { get; set; }
        public virtual FileUpload? FileUpload { get; set; }
    }
}
