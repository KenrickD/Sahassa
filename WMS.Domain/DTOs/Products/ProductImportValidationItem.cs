namespace WMS.Domain.DTOs.Products
{
    public class ProductImportValidationItem
    {
        public int RowNumber { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        // Product data
        public string ClientCode { get; set; } = string.Empty;
        public Guid ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Description { get; set; }
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
        public bool IsActive { get; set; } = true;
        public bool IsHazardous { get; set; }
        public bool IsFragile { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;
        public Guid ProductTypeId { get; set; }
        public string? ProductCategoryName { get; set; }
        public Guid? ProductCategoryId { get; set; }
        public string? UnitOfMeasureName { get; set; }
        public Guid? UnitOfMeasureId { get; set; }
    }
}
