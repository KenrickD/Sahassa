
namespace WMS.Domain.DTOs.Products
{
    public class ProductImportResultItem
    {
        public int RowNumber { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Success, Error, Warning
        public string Message { get; set; } = string.Empty;
    }
}
