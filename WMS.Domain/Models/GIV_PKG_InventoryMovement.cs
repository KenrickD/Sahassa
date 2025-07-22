using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_PKG_InventoryMovement")]
    public class GIV_PKG_InventoryMovement : TenantEntity
    {
        public Guid GIVPKGInventoryId { get; set; }
        public virtual GIV_PKG_Inventory GIVPKGInventory { get; set; } = null!;
        public Guid? FromLocationId { get; set; }
        public virtual Location FromLocation { get; set; } = null!;
        public Guid? ToLocationId { get; set; }
        public virtual Location ToLocation { get; set; } = null!;
        public int FromLocationAddressId { get; set; } //from onpremise API get from TB_AddressLookup
        public int ToLocationAddressId { get; set; } //from onpremise API get from TB_AddressLookup
        public MovementType Type { get; set; }
        public DateTime MovementDate { get; set; }
        public int Qty { get; set; }
        public int PackSize { get; set; }
        public int NoItems { get; set; }
        public int NoOfPallet { get; set; }
        public bool IsReadyToBill { get; set; }
        public bool IsBilled { get; set; }
        [MaxLength(500)]
        public string? Remarks { get; set; }
    }
}
