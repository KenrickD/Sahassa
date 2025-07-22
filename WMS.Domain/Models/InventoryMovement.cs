
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.Models
{
    [Table("TB_InventoryMovement")]
    public class InventoryMovement : TenantEntity
    {
        //public Guid InventoryId { get; set; }
        //public virtual Inventory Inventory { get; set; } = null!;
        public Guid EntityId { get; set; }
        public string EntityName { get; set; } = default!;
        public Guid? FromLocationId { get; set; }
        public virtual Location FromLocation { get; set; } = null!;
        public Guid? ToLocationId { get; set; }
        public virtual Location ToLocation { get; set; } = null!;
        public decimal Quantity { get; set; }
        public MovementType Type { get; set; }
        [MaxLength(100)]
        public string ReferenceNumber { get; set; } = default!; // PO number, Order number, etc.
        [Required]
        [MaxLength(100)]
        public string PerformedBy { get; set; } = default!;
        public DateTime MovementDate { get; set; }
        [MaxLength(500)]
        public string? Remarks { get; set; }
    }
}
