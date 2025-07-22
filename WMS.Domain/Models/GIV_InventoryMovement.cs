using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_InventoryMovement")]
    public class GIV_InventoryMovement:TenantEntity
    {
        public Guid FG_ID { get; set; }
        public virtual GIV_FinishedGood FinishedGood { get; set; } = null!;
        public Guid? FromLocationId { get; set; }
        public virtual Location FromLocation { get; set; } = null!;
        public Guid? ToLocationId { get; set; }
        public virtual Location ToLocation { get; set; } = null!;
        public decimal Quantity { get; set; }
        public MovementType Type { get; set; }
        [Required]
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
