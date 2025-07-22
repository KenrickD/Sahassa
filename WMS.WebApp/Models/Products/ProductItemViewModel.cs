namespace WMS.WebApp.Models.Products
{
    public class ProductItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Category { get; set; }
        public string? UnitOfMeasure { get; set; }
        public bool IsActive { get; set; }
        public bool IsHazardous { get; set; }
        public bool IsFragile { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ProductTypeName { get; set; } = string.Empty;
        public string ProductCategoryName { get; set; } = string.Empty;
        public string UnitOfMeasureCodeName { get; set; } = string.Empty;
        public int InventoryCount { get; set; }
        public decimal CurrentStockLevel { get; set; }
        public decimal MinStockLevel { get; set; }
        public decimal ReorderPoint { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        // Display properties
        public string StatusDisplay => IsActive ? "Active" : "Inactive";
        public string StockStatusDisplay => GetStockStatusDisplay();
        public string StockStatusColor => GetStockStatusColor();

        private string GetStockStatusDisplay()
        {
            if (CurrentStockLevel <= 0) return "Out of Stock";
            if (CurrentStockLevel <= ReorderPoint) return "Reorder Needed";
            if (CurrentStockLevel <= MinStockLevel) return "Low Stock";
            return "In Stock";
        }

        private string GetStockStatusColor()
        {
            if (CurrentStockLevel <= 0) return "text-red-600";
            if (CurrentStockLevel <= ReorderPoint) return "text-orange-600";
            if (CurrentStockLevel <= MinStockLevel) return "text-yellow-600";
            return "text-green-600";
        }
    }
}
