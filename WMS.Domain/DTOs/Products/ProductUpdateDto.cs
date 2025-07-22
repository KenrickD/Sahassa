using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Products
{
    public class ProductUpdateDto
    {
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
        public Guid ProductTypeId { get; set; }
        public Guid? ProductCategoryId { get; set; }
        public Guid? UnitOfMeasureCodeId { get; set; }
        public bool IsActive { get; set; }
        public bool IsHazardous { get; set; }
        public bool IsFragile { get; set; }

        // For image upload
        public IFormFile? ProductImage { get; set; }
        public bool RemoveProductImage { get; set; }
    }
}
