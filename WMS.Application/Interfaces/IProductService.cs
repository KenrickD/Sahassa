using Microsoft.AspNetCore.Http;
using WMS.Domain.DTOs.Products;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.GeneralCodes;

namespace WMS.Application.Interfaces
{
    public interface IProductService
    {
        // Pagination method
        Task<PaginatedResult<ProductDto>> GetPaginatedProducts(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending);

        // Standard CRUD operations
        Task<List<ProductDto>> GetAllProductsAsync();
        Task<List<ProductDto>> GetProductsByClientIdAsync(Guid clientId);
        Task<ProductDto> GetProductByIdAsync(Guid id);
        Task<ProductDto> CreateProductAsync(ProductCreateDto productDto);
        Task<ProductDto> UpdateProductAsync(Guid id, ProductUpdateDto productDto);
        Task<bool> DeleteProductAsync(Guid id);
        Task<bool> ActivateProductAsync(Guid id, bool isActive);

        // Validation methods
        Task<bool> ProductExistsAsync(Guid id);
        Task<bool> ProductNameExistsAsync(string name, Guid clientId, Guid? excludeId = null);
        Task<bool> ProductSKUExistsAsync(string sku, Guid clientId, Guid? excludeId = null);
        Task<bool> ProductBarcodeExistsAsync(string barcode, Guid? excludeId = null);

        // Additional methods
        Task<List<ProductDto>> GetProductsByWarehouseIdAsync(Guid warehouseId, bool activeOnly = false);
        Task<List<ProductDto>> GetProductsByCategoryAsync(string category, Guid? clientId = null);
        Task<List<ProductDto>> GetLowStockProductsAsync(Guid? clientId = null);
        Task<List<ProductDto>> GetProductsRequiringReorderAsync(Guid? clientId = null);
        Task<int> GetProductCountByClientAsync(Guid clientId);

        // Search operations
        Task<List<ProductDto>> SearchProductsAsync(string searchTerm, Guid? clientId = null, string? category = null, bool? isActive = null);

        // Reporting methods
        Task<Dictionary<string, int>> GetProductCountByStatusAsync(Guid? clientId = null);
        Task<Dictionary<string, int>> GetProductCountByCategoryAsync(Guid? clientId = null);

        // Image operations
        Task<string> UploadProductImageAsync(IFormFile file, string? existingImagePath = null);
        string GetProductImageUrl(string? imagePath);

        // Import/Export methods
        byte[] GenerateProductTemplate(List<GeneralCodeDto> productTypes, List<GeneralCodeDto> productCategories, List<GeneralCodeDto> unitOfMeasures);
        Task<byte[]> ExportProductsAsync(Guid? clientId = null);
        Task<ProductImportValidationResult> ValidateProductImportAsync(IFormFile file);
        Task<ProductImportResult> ImportProductsAsync(IFormFile file);
    }
}
