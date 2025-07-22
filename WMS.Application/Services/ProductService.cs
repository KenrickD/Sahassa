using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WMS.Application.Extensions;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.GeneralCodes;
using WMS.Domain.DTOs.Products;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;

namespace WMS.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IGeneralCodeService _generalCodeService;
        private readonly IDateTime _dateTime;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            AppDbContext dbContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IGeneralCodeService generalCodeService,
            IDateTime dateTime,
            IWebHostEnvironment environment,
            ILogger<ProductService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _generalCodeService = generalCodeService;
            _dateTime = dateTime;
            _environment = environment;
            _logger = logger;
        }

        public async Task<PaginatedResult<ProductDto>> GetPaginatedProducts(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated products: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
                searchTerm, skip, take, sortColumn, sortAscending);

            try
            {
                // Start with base query using tenant filter
                var query = _dbContext.Products
                    .ApplyTenantFilter(_currentUserService)
                    .Include(p => p.Client)
                    .Include(p => p.ProductType)
                    .Include(p => p.ProductCategory)
                    .Include(p => p.UnitOfMeasureCode)
                    .AsQueryable();

                // Apply search if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    _logger.LogDebug("Applying search filter: {SearchTerm}", searchTerm);

                    query = query.Where(p =>
                        p.Name.ToLower().Contains(searchTerm) ||
                        p.SKU.ToLower().Contains(searchTerm) ||
                        (p.Client != null && p.Client.Name.ToLower().Contains(searchTerm)) ||
                        (p.ProductType != null && p.ProductType.Name.ToLower().Contains(searchTerm)) ||
                        (p.ProductCategory != null && p.ProductCategory.Name.ToLower().Contains(searchTerm)) ||
                        (p.UnitOfMeasureCode != null && p.UnitOfMeasureCode.Name.ToLower().Contains(searchTerm)) ||
                        (p.Barcode != null && p.Barcode.ToLower().Contains(searchTerm)) ||
                        (p.SubCategory != null && p.SubCategory.ToLower().Contains(searchTerm))
                    );
                }

                // Apply sorting
                query = ApplySorting(query, sortColumn, !sortAscending);

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                _logger.LogDebug("Total products matching criteria: {TotalCount}", totalCount);

                // Apply pagination
                var products = await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var productDtos = _mapper.Map<List<ProductDto>>(products);

                // Get inventory counts and stock levels for each product
                foreach (var productDto in productDtos)
                {
                    productDto.InventoryCount = await _dbContext.Inventories
                        .CountAsync(i => i.ProductId == productDto.Id && !i.IsDeleted);

                    productDto.CurrentStockLevel = await _dbContext.Inventories
                        .Where(i => i.ProductId == productDto.Id && !i.IsDeleted)
                        .SumAsync(i => i.Quantity);
                }

                _logger.LogInformation("Retrieved {ProductCount} paginated products (skip={Skip}, take={Take}) from total of {TotalCount}",
                    productDtos.Count, skip, take, totalCount);

                return new PaginatedResult<ProductDto>
                {
                    Items = productDtos,
                    TotalCount = totalCount,
                    FilteredCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated products: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private IQueryable<Product> ApplySorting(IQueryable<Product> query, int sortColumn, bool sortDescending)
        {
            return sortColumn switch
            {
                1 => sortDescending ? query.OrderByDescending(p => p.Client.Name) : query.OrderBy(p => p.Client.Name),
                2 => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                3 => sortDescending ? query.OrderByDescending(p => p.SKU) : query.OrderBy(p => p.SKU),
                4 => sortDescending ? query.OrderByDescending(p => p.ProductType.Name) : query.OrderBy(p => p.ProductType.Name),
                5 => sortDescending ? query.OrderByDescending(p => p.ProductCategory.Name) : query.OrderBy(p => p.ProductCategory.Name),
                6 => sortDescending ? query.OrderByDescending(p => p.UnitOfMeasureCode.Name) : query.OrderBy(p => p.UnitOfMeasureCode.Name),
                7 => sortDescending ? query.OrderByDescending(p => p.IsActive) : query.OrderBy(p => p.IsActive),
                _ => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
            };
        }

        public async Task<ProductDto> CreateProductAsync(ProductCreateDto productDto)
        {
            _logger.LogInformation("Creating new product: {ProductName}", productDto.Name);

            // Validate unique constraints
            if (await ProductNameExistsAsync(productDto.Name, productDto.ClientId))
            {
                var message = $"Product name '{productDto.Name}' already exists for this client";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (await ProductSKUExistsAsync(productDto.SKU, productDto.ClientId))
            {
                var message = $"Product SKU '{productDto.SKU}' already exists for this client";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (!string.IsNullOrEmpty(productDto.Barcode) && await ProductBarcodeExistsAsync(productDto.Barcode))
            {
                var message = $"Product barcode '{productDto.Barcode}' already exists";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            var client = await _dbContext.Clients.FindAsync(productDto.ClientId);

            if (client == null)
            {
                var message = $"Selected client not found in database, try again.";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var product = _mapper.Map<Product>(productDto);
                product.Id = Guid.NewGuid();
                product.WarehouseId = client.WarehouseId;

                // Handle product image upload
                if (productDto.ProductImage != null)
                {
                    product.ImageUrl = await UploadProductImageAsync(productDto.ProductImage);
                }

                await _dbContext.AddAsync(product);
                await _dbContext.SaveChangesAsync();

                // Reload product with client information
                await _dbContext.Entry(product).Reference(p => p.Client).LoadAsync();

                await transaction.CommitAsync();

                var result = _mapper.Map<ProductDto>(product);
                result.InventoryCount = 0; // New product has no inventory
                result.CurrentStockLevel = 0;

                _logger.LogInformation("Product created successfully: {ProductId} - {ProductName}",
                    result.Id, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating product: {ProductName}", productDto.Name);
                throw;
            }
        }

        public async Task<ProductDto> UpdateProductAsync(Guid id, ProductUpdateDto productDto)
        {
            _logger.LogInformation("Updating product: {ProductId} - {ProductName}", id, productDto.Name);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var product = await _dbContext.Products
                    .ApplyTenantFilter(_currentUserService)
                    .Include(p => p.Client)
                    .Include(p => p.ProductType)
                    .Include(p => p.ProductCategory)
                    .Include(p => p.UnitOfMeasureCode)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    var message = $"Product with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                // Validate unique constraints
                if (await ProductNameExistsAsync(productDto.Name, product.ClientId, id))
                {
                    var message = $"Product name '{productDto.Name}' already exists for this client";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }

                if (await ProductSKUExistsAsync(productDto.SKU, product.ClientId, id))
                {
                    var message = $"Product SKU '{productDto.SKU}' already exists for this client";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }

                if (!string.IsNullOrEmpty(productDto.Barcode) && await ProductBarcodeExistsAsync(productDto.Barcode, id))
                {
                    var message = $"Product barcode '{productDto.Barcode}' already exists";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }

                // Handle product image
                if (productDto.ProductImage != null)
                {
                    product.ImageUrl = await UploadProductImageAsync(productDto.ProductImage, product.ImageUrl);
                }
                else if (productDto.RemoveProductImage)
                {
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        DeleteProductImage(product.ImageUrl);
                        product.ImageUrl = null;
                    }
                }

                // Update product properties
                _mapper.Map(productDto, product);
                product.ModifiedBy = _currentUserService.UserId;
                product.ModifiedAt = _dateTime.Now;

                _dbContext.Update(product);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var result = _mapper.Map<ProductDto>(product);

                // Get inventory count and stock level
                result.InventoryCount = await _dbContext.Inventories
                    .CountAsync(i => i.ProductId == id && !i.IsDeleted);
                result.CurrentStockLevel = await _dbContext.Inventories
                    .Where(i => i.ProductId == id && !i.IsDeleted)
                    .SumAsync(i => i.Quantity);

                _logger.LogInformation("Product updated successfully: {ProductId} - {ProductName}",
                    result.Id, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating product: {ProductId} - {ProductName}", id, productDto.Name);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            _logger.LogInformation("Deleting product: {ProductId}", id);

            try
            {
                var product = await _dbContext.Products
                    .ApplyTenantFilter(_currentUserService)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    var message = $"Product with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                // Check if product has associated inventory
                var hasInventory = await _dbContext.Inventories.AnyAsync(i => i.ProductId == id && !i.IsDeleted);

                if (hasInventory)
                {
                    var message = $"Cannot delete product '{product.Name}' as it has associated inventory";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }

                product.IsDeleted = true;
                product.ModifiedBy = _currentUserService.UserId;
                product.ModifiedAt = _dateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Product deleted successfully: {ProductId} - {ProductName}",
                    id, product.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                throw;
            }
        }

        // Image operations
        public async Task<string> UploadProductImageAsync(IFormFile file, string? existingImagePath = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file", nameof(file));

            // Validate file type
            if (!IsValidImageFile(file))
                throw new ArgumentException("Invalid image file type. Only JPG, PNG, and GIF are allowed.");

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size exceeds the 5MB limit.");

            // Delete existing image if it exists
            if (!string.IsNullOrEmpty(existingImagePath))
            {
                DeleteProductImage(existingImagePath);
            }

            // Create upload directory if it doesn't exist
            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadDir);

            // Generate unique filename with original extension
            var fileName = $"{Guid.NewGuid()}_{DateTime.UtcNow.Ticks}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path
            return $"/uploads/products/{fileName}";
        }

        public string GetProductImageUrl(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return "/images/default-product.png"; // Default product image
            }

            // If path is already a URL, return it
            if (imagePath.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                return imagePath;
            }

            // Otherwise, return relative path
            return imagePath;
        }

        private bool IsValidImageFile(IFormFile file)
        {
            // Check file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return false;

            // Check MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };

            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return false;

            return true;
        }

        private void DeleteProductImage(string imagePath)
        {
            try
            {
                // Clean up the path to ensure it's a relative path
                var relativePath = imagePath.TrimStart('/');
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

                // Check if file exists
                if (File.Exists(fullPath))
                {
                    // Delete the file
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted product image: {Path}", fullPath);
                }
                else
                {
                    _logger.LogWarning("Could not delete product image: {Path} (file not found)", fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product image: {Path}", imagePath);
            }
        }

        // Additional implementation methods...
        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            var products = await _dbContext.Products
                .ApplyTenantFilter(_currentUserService)
                .Include(p => p.Client)
                .Include(p => p.ProductType)
                .Include(p => p.ProductCategory)
                .Include(p => p.UnitOfMeasureCode)
                .ToListAsync();

            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<ProductDto> GetProductByIdAsync(Guid id)
        {
            var product = await _dbContext.Products
                .ApplyTenantFilter(_currentUserService)
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {id} not found.");
            }

            var result = _mapper.Map<ProductDto>(product);
            result.InventoryCount = await _dbContext.Inventories
                .CountAsync(i => i.ProductId == id && !i.IsDeleted);
            result.CurrentStockLevel = await _dbContext.Inventories
                .Where(i => i.ProductId == id && !i.IsDeleted)
                .SumAsync(i => i.Quantity);

            return result;
        }

        public async Task<List<ProductDto>> GetProductsByClientIdAsync(Guid clientId)
        {
            var products = await _dbContext.Products
                .ApplyTenantFilter(_currentUserService)
                .Include(p => p.Client)
                .Include(p => p.ProductType)
                .Include(p => p.ProductCategory)
                .Include(p => p.UnitOfMeasureCode)
                .Where(p => p.ClientId == clientId)
                .ToListAsync();

            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<bool> ActivateProductAsync(Guid id, bool isActive) =>
            await UpdateProductPropertyAsync(id, p => p.IsActive = isActive);

        public async Task<bool> ProductExistsAsync(Guid id)
        {
            return await _dbContext.Products
                .ApplyTenantFilter(_currentUserService)
                .AnyAsync(p => p.Id == id);
        }

        public async Task<bool> ProductNameExistsAsync(string name, Guid clientId, Guid? excludeId = null)
        {
            return await _dbContext.Products
                .AnyAsync(p => p.Name == name &&
                              p.ClientId == clientId &&
                              !p.IsDeleted &&
                              (excludeId == null || p.Id != excludeId));
        }

        public async Task<bool> ProductSKUExistsAsync(string sku, Guid clientId, Guid? excludeId = null)
        {
            return await _dbContext.Products
                .AnyAsync(p => p.SKU == sku &&
                              p.ClientId == clientId &&
                              !p.IsDeleted &&
                              (excludeId == null || p.Id != excludeId));
        }

        public async Task<bool> ProductBarcodeExistsAsync(string barcode, Guid? excludeId = null)
        {
            if (string.IsNullOrEmpty(barcode))
                return false;

            var warehouseId = _currentUserService.CurrentWarehouseId;
            return await _dbContext.Products
                .AnyAsync(p => p.Barcode == barcode &&
                              p.WarehouseId == warehouseId &&
                              !p.IsDeleted &&
                              (excludeId == null || p.Id != excludeId));
        }

        public async Task<List<ProductDto>> GetProductsByWarehouseIdAsync(Guid warehouseId, bool activeOnly = false)
        {
            var query = _dbContext.Products
                .Include(p => p.Client)
                .Where(p => p.WarehouseId == warehouseId && !p.IsDeleted);

            if (activeOnly)
            {
                query = query.Where(p => p.IsActive);
            }

            var products = await query.ToListAsync();
            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<List<ProductDto>> GetProductsByCategoryAsync(string category, Guid? clientId = null)
        {
            var query = _dbContext.Products
                .ApplyTenantFilter(_currentUserService)
                .Include(p => p.Client)
                .Include(p => p.ProductType)
                .Include(p => p.ProductCategory)
                .Include(p => p.UnitOfMeasureCode)
                .Where(p => p.ProductCategory.Name == category && !p.IsDeleted);

            if (clientId.HasValue)
            {
                query = query.Where(p => p.ClientId == clientId.Value);
            }

            var products = await query.ToListAsync();
            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<List<ProductDto>> GetLowStockProductsAsync(Guid? clientId = null)
        {
            var query = _dbContext.Products
                .ApplyTenantFilter(_currentUserService)
                .Include(p => p.Client)
                .Include(p => p.ProductType)
                .Include(p => p.ProductCategory)
                .Include(p => p.UnitOfMeasureCode)
                .Where(p => !p.IsDeleted && p.IsActive);

            if (clientId.HasValue)
            {
                query = query.Where(p => p.ClientId == clientId.Value);
            }

            var products = await query.ToListAsync();
            var lowStockProducts = new List<Product>();

            foreach (var product in products)
            {
                var currentStock = await _dbContext.Inventories
                    .Where(i => i.ProductId == product.Id && !i.IsDeleted)
                    .SumAsync(i => i.Quantity);

                //if (currentStock <= product.MinStockLevel)
                //{
                //    lowStockProducts.Add(product);
                //}
            }

            return _mapper.Map<List<ProductDto>>(lowStockProducts);
        }

        public async Task<List<ProductDto>> GetProductsRequiringReorderAsync(Guid? clientId = null)
        {
            var query = _dbContext.Products
                .ApplyTenantFilter(_currentUserService)
                .Include(p => p.Client)
                .Include(p => p.ProductType)
                .Include(p => p.ProductCategory)
                .Include(p => p.UnitOfMeasureCode)
                .Where(p => !p.IsDeleted && p.IsActive);

            if (clientId.HasValue)
            {
                query = query.Where(p => p.ClientId == clientId.Value);
            }

            var products = await query.ToListAsync();
            var reorderProducts = new List<Product>();

            foreach (var product in products)
            {
                var currentStock = await _dbContext.Inventories
                    .Where(i => i.ProductId == product.Id && !i.IsDeleted)
                    .SumAsync(i => i.Quantity);

                //if (currentStock <= product.ReorderPoint)
                //{
                //    reorderProducts.Add(product);
                //}
            }

            return _mapper.Map<List<ProductDto>>(reorderProducts);
        }

        public async Task<int> GetProductCountByClientAsync(Guid clientId)
        {
            return await _dbContext.Products
                .CountAsync(p => p.ClientId == clientId && !p.IsDeleted && p.IsActive);
        }

        public async Task<List<ProductDto>> SearchProductsAsync(string searchTerm, Guid? clientId = null, string? category = null, bool? isActive = null)
        {
            var query = _dbContext.Products
                .ApplyTenantFilter(_currentUserService)
                .Include(p => p.Client)
                .Include(p => p.ProductType)
                .Include(p => p.ProductCategory)
                .Include(p => p.UnitOfMeasureCode)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.SKU.ToLower().Contains(searchTerm) ||
                    (p.Barcode != null && p.Barcode.ToLower().Contains(searchTerm)));
            }

            if (clientId.HasValue)
            {
                query = query.Where(p => p.ClientId == clientId.Value);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.ProductCategory.Name == category);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            var products = await query.ToListAsync();
            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<Dictionary<string, int>> GetProductCountByStatusAsync(Guid? clientId = null)
        {
            var query = _dbContext.Products.ApplyTenantFilter(_currentUserService);
            if (clientId.HasValue) query = query.Where(p => p.ClientId == clientId);

            return new Dictionary<string, int>
            {
                ["Active"] = await query.CountAsync(p => p.IsActive && !p.IsDeleted),
                ["Inactive"] = await query.CountAsync(p => !p.IsActive && !p.IsDeleted),
                ["Total"] = await query.CountAsync(p => !p.IsDeleted)
            };
        }

        public async Task<Dictionary<string, int>> GetProductCountByCategoryAsync(Guid? clientId = null)
        {
            var query = _dbContext.Products.ApplyTenantFilter(_currentUserService);
            if (clientId.HasValue) query = query.Where(p => p.ClientId == clientId);

            return await query
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.ProductCategory.Name))
                .GroupBy(p => p.ProductCategory.Name)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        private async Task<bool> UpdateProductPropertyAsync(Guid productId, System.Action<Product> updateAction)
        {
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null) return false;

            updateAction(product);
            product.ModifiedBy = _currentUserService.UserId;
            product.ModifiedAt = _dateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        #region Import/Export Methods

        public byte[] GenerateProductTemplate(List<GeneralCodeDto> productTypes, List<GeneralCodeDto> productCategories, List<GeneralCodeDto> unitOfMeasures)
        {
            _logger.LogInformation("Generating product import template");

            try
            {
                var file = ExcelHelper.GenerateProductImportTemplate(productTypes, productCategories, unitOfMeasures);
                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating product template");
                throw;
            }
        }

        public async Task<byte[]> ExportProductsAsync(Guid? clientId = null)
        {
            _logger.LogInformation("Exporting products, ClientId: {ClientId}", clientId);

            try
            {
                var query = _dbContext.Products
                    .ApplyTenantFilter(_currentUserService)
                    .Include(p => p.Client)
                    .Include(p => p.ProductType)
                    .Include(p => p.ProductCategory)
                    .Include(p => p.UnitOfMeasureCode)
                    .Include(p => p.Inventories)
                    .AsQueryable();

                if (clientId.HasValue)
                {
                    query = query.Where(p => p.ClientId == clientId.Value);
                }

                var products = await query.OrderBy(p => p.Client.Name).ThenBy(p => p.Name).ToListAsync();

                var productDtos = _mapper.Map<List<ProductDto>>(products);
                foreach (var productDto in productDtos)
                {
                    productDto.InventoryCount = products.Where(p => p.Id == productDto.Id)
                        .SelectMany(p => p.Inventories ?? new List<Inventory>())
                        .Count();
                    productDto.CurrentStockLevel = products.Where(p => p.Id == productDto.Id)
                        .SelectMany(p => p.Inventories ?? new List<Inventory>())
                        .Sum(i => i.Quantity);
                }

                var excelFile = ExcelHelper.ExportProducts(productDtos);

                _logger.LogInformation("Exported {Count} products", products.Count);
                return excelFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products");
                throw;
            }
        }

        public async Task<ProductImportValidationResult> ValidateProductImportAsync(IFormFile file)
        {
            _logger.LogInformation("Validating product import file: {FileName}", file.FileName);

            var result = new ProductImportValidationResult();

            try
            {
                ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");

                using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    result.Errors.Add("No worksheet found in the Excel file");
                    return result;
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount <= 1)
                {
                    result.Errors.Add("No data rows found in the Excel file");
                    return result;
                }

                result.TotalRows = rowCount - 1; // Exclude header row

                // Get all clients for validation
                var clients = await _dbContext.Clients
                    .ApplyTenantFilter(_currentUserService)
                    .ToListAsync();

                var clientDict = clients.ToDictionary(c => c.Code ?? c.Name, c => c, StringComparer.OrdinalIgnoreCase);

                List<string> codeTypes = new List<string>
                {
                    AppConsts.GeneralCodeType.PRODUCT_TYPE,
                    AppConsts.GeneralCodeType.PRODUCT_CATEGORY,
                    AppConsts.GeneralCodeType.PRODUCT_UOM,
                };
              
                var generalCodeDtos = await _generalCodeService.GetCodesByTypesAsync(codeTypes);

                // Validate each row
                for (int row = 2; row <= rowCount; row++)
                {
                    var validationItem = await ValidateProductRow(worksheet, row, clientDict, generalCodeDtos);

                    if (validationItem.IsValid)
                    {
                        result.ValidItems.Add(validationItem);
                    }
                    else
                    {
                        result.Errors.AddRange(validationItem.Errors.Select(e => $"Row {row}: {e}"));
                    }

                    if (validationItem.Warnings.Any())
                    {
                        result.Warnings.AddRange(validationItem.Warnings.Select(w => $"Row {row}: {w}"));
                    }
                }

                result.IsValid = !result.Errors.Any();

                _logger.LogInformation("Validation completed. Valid: {IsValid}, Total: {Total}, Valid Items: {ValidCount}, Errors: {ErrorCount}",
                    result.IsValid, result.TotalRows, result.ValidItems.Count, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating product import file");
                result.Errors.Add($"File validation failed: {ex.Message}");
                return result;
            }
        }

        public async Task<ProductImportResult> ImportProductsAsync(IFormFile file)
        {
            _logger.LogInformation("Importing products from file: {FileName}", file.FileName);

            var result = new ProductImportResult();

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // First validate the file
                var validationResult = await ValidateProductImportAsync(file);

                result.TotalRows = validationResult.TotalRows;
                result.Errors.AddRange(validationResult.Errors);
                result.Warnings.AddRange(validationResult.Warnings);

                if (!validationResult.IsValid)
                {
                    result.Success = false;
                    return result;
                }

                // Process valid items
                var productsToCreate = new List<Product>();

                foreach (var validItem in validationResult.ValidItems)
                {
                    try
                    {
                        var product = new Product
                        {
                            Id = Guid.NewGuid(),
                            ClientId = validItem.ClientId,
                            WarehouseId = _currentUserService.CurrentWarehouseId,
                            Name = validItem.Name,
                            SKU = validItem.SKU,
                            Barcode = validItem.Barcode,
                            Description = validItem.Description,
                            Weight = validItem.Weight,
                            Length = validItem.Length,
                            Width = validItem.Width,
                            Height = validItem.Height,
                            //UnitOfMeasure = validItem.UnitOfMeasure,
                            RequiresLotTracking = validItem.RequiresLotTracking,
                            RequiresExpirationDate = validItem.RequiresExpirationDate,
                            RequiresSerialNumber = validItem.RequiresSerialNumber,
                            //MinStockLevel = validItem.MinStockLevel,
                            //MaxStockLevel = validItem.MaxStockLevel,
                            //ReorderPoint = validItem.ReorderPoint,
                            //ReorderQuantity = validItem.ReorderQuantity,
                            //Category = validItem.Category,
                            SubCategory = validItem.SubCategory,
                            IsActive = validItem.IsActive,
                            IsHazardous = validItem.IsHazardous,
                            IsFragile = validItem.IsFragile,
                            ProductTypeId = validItem.ProductTypeId,
                            ProductCategoryId = validItem.ProductCategoryId ?? Guid.Empty,
                            UnitOfMeasureCodeId = validItem.UnitOfMeasureId ?? Guid.Empty,
                            CreatedBy = _currentUserService.UserId,
                            CreatedAt = _dateTime.Now
                        };

                        productsToCreate.Add(product);
                        result.SuccessCount++;
                        result.Results.Add(new ProductImportResultItem
                        {
                            RowNumber = validItem.RowNumber,
                            ProductName = product.Name,
                            ProductSKU = product.SKU,
                            Status = "Success",
                            Message = "Product created successfully"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing valid product item: {ProductName}", validItem.Name);
                        result.ErrorCount++;
                        result.Errors.Add($"Row {validItem.RowNumber}: Failed to process product - {ex.Message}");
                        result.Results.Add(new ProductImportResultItem
                        {
                            RowNumber = validItem.RowNumber,
                            ProductName = validItem.Name,
                            ProductSKU = validItem.SKU,
                            Status = "Error",
                            Message = ex.Message
                        });
                    }
                }

                // Bulk insert products
                if (productsToCreate.Any())
                {
                    await _dbContext.Products.AddRangeAsync(productsToCreate);
                    await _dbContext.SaveChangesAsync();
                }

                result.ProcessedRows = result.SuccessCount + result.ErrorCount;
                result.Success = result.ErrorCount == 0;

                await transaction.CommitAsync();

                _logger.LogInformation("Product import completed. Success: {SuccessCount}, Errors: {ErrorCount}",
                    result.SuccessCount, result.ErrorCount);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error importing products");
                result.Success = false;
                result.Errors.Add($"Import failed: {ex.Message}");
                result.Results = new List<ProductImportResultItem>();
                result.SuccessCount = 0;
                return result;
            }
        }

        private async Task<ProductImportValidationItem> ValidateProductRow(ExcelWorksheet worksheet, int row, 
            Dictionary<string, Client> clientDict, List<GeneralCodeDto> generalCodeDtos)
        {
            var item = new ProductImportValidationItem { RowNumber = row };

            try
            {
                var productTypes = generalCodeDtos.Where(x => x.GeneralCodeTypeName == AppConsts.GeneralCodeType.PRODUCT_TYPE);
                var productCategories = generalCodeDtos.Where(x => x.GeneralCodeTypeName == AppConsts.GeneralCodeType.PRODUCT_CATEGORY);
                var unitOfMeasures = generalCodeDtos.Where(x => x.GeneralCodeTypeName == AppConsts.GeneralCodeType.PRODUCT_UOM);

                // Required fields validation
                var clientCode = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                var productType = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                var productName = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                var productSKU = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                var productCategory = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                var unitOfMeasure = worksheet.Cells[row, 9].Value?.ToString()?.Trim();

                if (string.IsNullOrEmpty(clientCode))
                {
                    item.Errors.Add("Client Code is required");
                }
                else if (!clientDict.ContainsKey(clientCode))
                {
                    item.Errors.Add($"Client '{clientCode}' not found");
                }
                else
                {
                    item.ClientId = clientDict[clientCode].Id;
                    item.ClientCode = clientCode;
                }

                if (string.IsNullOrEmpty(productType))
                {
                    item.Errors.Add("Product type is required");
                }
                else if (!productTypes.Any(x => x.Name == productType))
                {
                    item.Errors.Add($"Product type '{productType}' not found");
                }
                else
                {
                    item.ProductTypeId = productTypes.First(x => x.Name == productType).Id;
                    item.ProductTypeName = productType;
                }

                if (string.IsNullOrEmpty(productCategory))
                {
                    item.Errors.Add("Product category is required");
                }
                else if (!productCategories.Any(x => x.Name == productCategory))
                {
                    item.Errors.Add($"Product category '{productCategory}' not found");
                }
                else
                {
                    item.ProductCategoryId = productCategories.First(x => x.Name == productCategory).Id;
                    item.ProductCategoryName = productCategory;
                }


                if (string.IsNullOrEmpty(unitOfMeasure))
                {
                    item.Errors.Add("Product unit of measure is required");
                }
                else if (!unitOfMeasures.Any(x => x.Name == unitOfMeasure))
                {
                    item.Errors.Add($"Product unit of measure '{unitOfMeasure}' not found");
                }
                else
                {
                    item.UnitOfMeasureId = unitOfMeasures.First(x => x.Name == unitOfMeasure).Id;
                    item.UnitOfMeasureName = unitOfMeasure;
                }

                if (string.IsNullOrEmpty(productName))
                {
                    item.Errors.Add("Product Name is required");
                }
                else
                {
                    item.Name = productName;

                    // Check if product name already exists for this client
                    if (item.ClientId != Guid.Empty)
                    {
                        var existingProduct = await _dbContext.Products
                            .FirstOrDefaultAsync(p => p.Name == productName && p.ClientId == item.ClientId && !p.IsDeleted);
                        if (existingProduct != null)
                        {
                            item.Errors.Add($"Product name '{productName}' already exists for client '{clientCode}'");
                        }
                    }
                }

                if (string.IsNullOrEmpty(productSKU))
                {
                    item.Errors.Add("Product SKU is required");
                }
                else
                {
                    item.SKU = productSKU;

                    // Check if SKU already exists for this client
                    if (item.ClientId != Guid.Empty)
                    {
                        var existingProduct = await _dbContext.Products
                            .FirstOrDefaultAsync(p => p.SKU == productSKU && p.ClientId == item.ClientId && !p.IsDeleted);
                        if (existingProduct != null)
                        {
                            item.Errors.Add($"Product SKU '{productSKU}' already exists for client '{clientCode}'");
                        }
                    }
                }

                // Optional fields
                item.Barcode = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                item.Description = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                item.SubCategory = worksheet.Cells[row, 8].Value?.ToString()?.Trim();

                // Numeric fields with validation
                if (decimal.TryParse(worksheet.Cells[row, 10].Value?.ToString(), out var weight))
                    item.Weight = weight;

                if (decimal.TryParse(worksheet.Cells[row, 11].Value?.ToString(), out var length))
                    item.Length = length;

                if (decimal.TryParse(worksheet.Cells[row, 12].Value?.ToString(), out var width))
                    item.Width = width;

                if (decimal.TryParse(worksheet.Cells[row, 13].Value?.ToString(), out var height))
                    item.Height = height;

                // Boolean fields
                var lotTrackingStr = worksheet.Cells[row, 14].Value?.ToString()?.Trim();
                item.RequiresLotTracking = !string.IsNullOrEmpty(lotTrackingStr) &&
                                         (lotTrackingStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                                          lotTrackingStr.Equals("1", StringComparison.OrdinalIgnoreCase));

                var expirationStr = worksheet.Cells[row, 15].Value?.ToString()?.Trim();
                item.RequiresExpirationDate = !string.IsNullOrEmpty(expirationStr) &&
                                            (expirationStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                                             expirationStr.Equals("1", StringComparison.OrdinalIgnoreCase));

                var serialStr = worksheet.Cells[row, 16].Value?.ToString()?.Trim();
                item.RequiresSerialNumber = !string.IsNullOrEmpty(serialStr) &&
                                          (serialStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                                           serialStr.Equals("1", StringComparison.OrdinalIgnoreCase));

                var hazardousStr = worksheet.Cells[row, 17].Value?.ToString()?.Trim();
                item.IsHazardous = !string.IsNullOrEmpty(hazardousStr) &&
                                 (hazardousStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                                  hazardousStr.Equals("1", StringComparison.OrdinalIgnoreCase));

                var fragileStr = worksheet.Cells[row, 18].Value?.ToString()?.Trim();
                item.IsFragile = !string.IsNullOrEmpty(fragileStr) &&
                               (fragileStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                                fragileStr.Equals("1", StringComparison.OrdinalIgnoreCase));

                var isActiveStr = worksheet.Cells[row, 19].Value?.ToString()?.Trim();
                item.IsActive = string.IsNullOrEmpty(isActiveStr) ||
                               isActiveStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                               isActiveStr.Equals("1", StringComparison.OrdinalIgnoreCase);

                item.IsValid = !item.Errors.Any();
            }
            catch (Exception ex)
            {
                item.Errors.Add($"Error processing row: {ex.Message}");
                item.IsValid = false;
            }

            return item;
        }

        #endregion
    }
}