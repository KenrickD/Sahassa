
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_Product")]
    public class Product : TenantEntity
    {
        public virtual ICollection<Inventory>? Inventories { get; set; }
        public virtual ICollection<GIV_PKG_InventoryMovement>? GIVPKGInventories { get; set; }
        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = default!;
        [Required]
        [MaxLength(100)]
        public string SKU { get; set; } = default!;
        [MaxLength(500)]
        public string? Barcode { get; set; }
        [MaxLength(500)]
        public string? Description { get; set; }
        public Guid ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;
        public decimal Weight { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        //[MaxLength(50)]
        //public string? UnitOfMeasure { get; set; }
        public bool RequiresLotTracking { get; set; }
        public bool RequiresExpirationDate { get; set; }
        public bool RequiresSerialNumber { get; set; }
        //public decimal MinStockLevel { get; set; }
        //public decimal MaxStockLevel { get; set; }
        //public decimal ReorderPoint { get; set; }
        //public decimal ReorderQuantity { get; set; }
        //[MaxLength(100)]
        //public string? Category { get; set; }
        [MaxLength(100)]
        public string? SubCategory { get; set; }
        public bool IsActive { get; set; }
        public bool IsHazardous { get; set; }
        public bool IsFragile { get; set; }
        [MaxLength(1000)]
        public string? ImageUrl { get; set; }
        public Guid ProductTypeId { get; set; }
        public virtual GeneralCode ProductType { get; set; } = null!;
        public Guid ProductCategoryId { get; set; }
        public virtual GeneralCode ProductCategory { get; set; } = null!;
        public Guid UnitOfMeasureCodeId { get; set; }
        public virtual GeneralCode UnitOfMeasureCode { get; set; } = null!;

    }
}
