
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_OrderLineAllocation")]
    public class OrderLineAllocation : TenantEntity
    {
        public Guid OrderLineId { get; set; }
        public virtual OrderLine OrderLine { get; set; } = null!;
        public Guid InventoryId { get; set; }
        public virtual Inventory Inventory { get; set; } = null!;
        public decimal AllocatedQuantity { get; set; }
        public decimal PickedQuantity { get; set; }
        public decimal PackedQuantity { get; set; }
        public decimal ShippedQuantity { get; set; }
        [MaxLength(100)]
        public string? AllocatedBy { get; set; }
        public DateTime? AllocationDate { get; set; }
    }
}
