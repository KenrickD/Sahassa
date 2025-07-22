namespace WMS.Domain.DTOs.Products
{
    public class ProductImportResult
    {
        public bool Success { get; set; }
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<ProductImportResultItem> Results { get; set; } = new();
    }
}
