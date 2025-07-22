
using static WMS.Domain.Enumerations.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_PKG_Inventory")]
    public class GIV_PKG_Inventory : TenantEntity
    {
        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;
        public Guid LocationId { get; set; }
        public virtual Location Location { get; set; } = null!;
        public Guid ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;
        public Guid? FileUploadId { get; set; }
        public virtual FileUpload? FileUpload { get; set; }
        public virtual ICollection<GIV_PKG_InventoryMovement> Movements { get; set; } = null!;
        [Required]
        [MaxLength(100)]
        public string BatchNo { get; set; } = default!;
        public InventoryStatus Status { get; set; }
        public int FromLocationAddressId { get; set; } //from onpremise API get from TB_AddressLookup
        public int ToLocationAddressId { get; set; } //from onpremise API get from TB_AddressLookup
        public DateTime? ReceiptDate { get; set; }
        public DateTime? PlanReceiveDate { get; set; }
        public DateTime? ActualReceiveDate { get; set; }
        public bool IsDG { get; set; }
        public int Qty { get; set; }
        public int PackSize { get; set; }
        public int NoItems { get; set; }
        public int NoOfPallet { get; set; }
        public int CurrentQty { get; set; }
        public int CurrentPackSize { get; set; }
        public int CurrentNoItems { get; set; }
        public int CurrentNoOfPallet { get; set; }
        public string? Remarks { get; set; }
    }
}
