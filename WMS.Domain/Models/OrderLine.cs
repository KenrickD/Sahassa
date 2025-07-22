
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_OrderLine")]
    public class OrderLine : TenantEntity
    {
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;
        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;
        public virtual ICollection<OrderLineAllocation>? Allocations { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityFulfilled { get; set; }
        [MaxLength(500)]
        public string? Remarks { get; set; }
        public int LineNumber { get; set; }
    }

}
