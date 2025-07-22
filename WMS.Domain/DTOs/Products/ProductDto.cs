
namespace WMS.Domain.DTOs.Products
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientCode { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string? UnitOfMeasure { get; set; }
        public bool RequiresLotTracking { get; set; }
        public bool RequiresExpirationDate { get; set; }
        public bool RequiresSerialNumber { get; set; }
        public decimal MinStockLevel { get; set; }
        public decimal MaxStockLevel { get; set; }
        public decimal ReorderPoint { get; set; }
        public decimal ReorderQuantity { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public bool IsActive { get; set; }
        public bool IsHazardous { get; set; }
        public bool IsFragile { get; set; }
        public string? ImageUrl { get; set; }
        public Guid ProductTypeId { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;
        public Guid ProductCategoryId { get; set; }
        public string ProductCategoryName { get; set; } = string.Empty;
        public Guid UnitOfMeasureCodeId { get; set; }
        public string UnitOfMeasureCodeName { get; set; } = string.Empty;
        public int InventoryCount { get; set; }
        public decimal CurrentStockLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        // this two for decide whether show and hide edit and delete button
        public bool HasWriteAccess { get; set; }
        public bool HasDeleteAccess { get; set; }
    }
}
