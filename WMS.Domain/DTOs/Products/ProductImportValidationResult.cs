namespace WMS.Domain.DTOs.Products
{
    public class ProductImportValidationResult
    {
        public bool IsValid { get; set; }
        public int TotalRows { get; set; }
        public List<ProductImportValidationItem> ValidItems { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
