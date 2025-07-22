using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.Products
{
    public class ProductViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Client is required")]
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(250, ErrorMessage = "Product name cannot exceed 250 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "SKU is required")]
        [StringLength(100, ErrorMessage = "SKU cannot exceed 100 characters")]
        public string SKU { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Barcode cannot exceed 500 characters")]
        public string? Barcode { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Weight must be a positive value")]
        public decimal Weight { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Length must be a positive value")]
        public decimal Length { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Width must be a positive value")]
        public decimal Width { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Height must be a positive value")]
        public decimal Height { get; set; }

        [StringLength(50, ErrorMessage = "Unit of measure cannot exceed 50 characters")]
        public string? UnitOfMeasure { get; set; }

        public bool RequiresLotTracking { get; set; }
        public bool RequiresExpirationDate { get; set; }
        public bool RequiresSerialNumber { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Min stock level must be a positive value")]
        public decimal MinStockLevel { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Max stock level must be a positive value")]
        public decimal MaxStockLevel { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Reorder point must be a positive value")]
        public decimal ReorderPoint { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Reorder quantity must be a positive value")]
        public decimal ReorderQuantity { get; set; }

        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string? Category { get; set; }

        [StringLength(100, ErrorMessage = "Sub category cannot exceed 100 characters")]
        public string? SubCategory { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsHazardous { get; set; }
        public bool IsFragile { get; set; }

        public string? ImageUrl { get; set; }
        public IFormFile? ProductImage { get; set; }
        public bool RemoveProductImage { get; set; }

        public Guid ProductTypeId { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;
        public Guid? ProductCategoryId { get; set; }
        public string ProductCategoryName { get; set; } = string.Empty;
        public Guid? UnitOfMeasureCodeId { get; set; }
        public string UnitOfMeasureCodeName { get; set; } = string.Empty;

        // Display properties
        public int InventoryCount { get; set; }
        public decimal CurrentStockLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Helper methods for display
        public string GetProductImageUrl()
        {
            return string.IsNullOrEmpty(ImageUrl) ? "/images/default-product.png" : ImageUrl;
        }

        public string GetStockStatusDisplay()
        {
            if (CurrentStockLevel <= 0) return "Out of Stock";
            if (CurrentStockLevel <= ReorderPoint) return "Reorder Needed";
            if (CurrentStockLevel <= MinStockLevel) return "Low Stock";
            return "In Stock";
        }

        public string GetStockStatusColor()
        {
            if (CurrentStockLevel <= 0) return "text-red-600";
            if (CurrentStockLevel <= ReorderPoint) return "text-orange-600";
            if (CurrentStockLevel <= MinStockLevel) return "text-yellow-600";
            return "text-green-600";
        }

        public decimal GetVolume()
        {
            return Length * Width * Height;
        }
    }
}