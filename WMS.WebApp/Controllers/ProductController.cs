using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.Products;
using WMS.Domain.Interfaces;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.Products;
using WMS.WebApp.Models.Clients;
using WMS.Domain.DTOs;
using WMS.WebApp.Models.GeneralCodes;
using WMS.Domain.DTOs.GeneralCodes;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IClientService _clientService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        private readonly IGeneralCodeService _generalCodeService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IClientService clientService,
            ICurrentUserService currentUserService,
            ToastService toastService,
            IGeneralCodeService generalCodeService,
            IMapper mapper,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _clientService = clientService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _mapper = mapper;
            _generalCodeService = generalCodeService;
            _logger = logger;
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.PRODUCT_READ}")]
        public IActionResult Index()
        {
            _logger.LogInformation("Product index page accessed by user {UserId}", _currentUserService.UserId);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetProducts(DataTablesRequest request)
        {
            _logger.LogDebug("Getting products: SearchTerm={SearchTerm}, Start={Start}, Length={Length}",
                request.Search?.Value, request.Start, request.Length);

            try
            {
                var result = await _productService.GetPaginatedProducts(
                    request.Search?.Value,
                    request.Start,
                    request.Length,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc"
                );

                _logger.LogDebug("Retrieved {Count} products out of {Total}", result.Items.Count, result.TotalCount);

                result.Items.ForEach(x =>
                {
                    x.HasWriteAccess = _currentUserService.HasPermission(AppConsts.Permissions.PRODUCT_WRITE);
                    x.HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.PRODUCT_DELETE);
                });

                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Items
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load products"
                });
            }
        }

        /// <summary>
        /// Display the create Product form
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.PRODUCT_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Creating new product - accessed by user {UserId}", _currentUserService.UserId);

            var viewModel = new ProductPageViewModel();
            viewModel.HasEditAccess = _currentUserService.HasPermission("Product.Write");

            // Load clients for dropdown
            await LoadDropdownItemAsync(viewModel);

            return View("Detail", viewModel);
        }

        /// <summary>
        /// Process the create Product form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel viewModel)
        {
            _logger.LogInformation("Creating product: {ProductName} by user {UserId}",
                viewModel.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to DTO
                    var productCreateDto = new ProductCreateDto
                    {
                        ClientId = viewModel.ClientId,
                        Name = viewModel.Name,
                        SKU = viewModel.SKU,
                        Barcode = viewModel.Barcode,
                        Description = viewModel.Description,
                        Weight = viewModel.Weight,
                        Length = viewModel.Length,
                        Width = viewModel.Width,
                        Height = viewModel.Height,
                        //UnitOfMeasure = viewModel.UnitOfMeasure,
                        RequiresLotTracking = viewModel.RequiresLotTracking,
                        RequiresExpirationDate = viewModel.RequiresExpirationDate,
                        RequiresSerialNumber = viewModel.RequiresSerialNumber,
                        //MinStockLevel = viewModel.MinStockLevel,
                        //MaxStockLevel = viewModel.MaxStockLevel,
                        //ReorderPoint = viewModel.ReorderPoint,
                        //ReorderQuantity = viewModel.ReorderQuantity,
                        //Category = viewModel.Category,
                        SubCategory = viewModel.SubCategory,
                        IsActive = viewModel.IsActive,
                        IsHazardous = viewModel.IsHazardous,
                        IsFragile = viewModel.IsFragile,
                        ProductTypeId = viewModel.ProductTypeId,
                        ProductCategoryId = viewModel.ProductCategoryId,
                        UnitOfMeasureCodeId = viewModel.UnitOfMeasureCodeId
                        //ProductImage = viewModel.ProductImage
                    };

                    // Create Product
                    var createdProduct = await _productService.CreateProductAsync(productCreateDto);

                    _logger.LogInformation("Product created successfully: {ProductId} - {ProductName} by user {UserId}",
                        createdProduct.Id, createdProduct.Name, _currentUserService.UserId);


                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    _toastService.AddSuccessToast("Product created successfully!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product: {ProductName} by user {UserId}",
                        viewModel.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error creating Product: {ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                    _toastService.AddErrorToast($"{ex.Message}");
                }
            }
            else if (Request.IsAjaxRequest())
            {
                return BadRequest(new { success = false, errors = ModelState });
            }
            else
            {
                _logger.LogWarning("Invalid model state when creating product by user {UserId}", _currentUserService.UserId);
            }

            var pageViewModel = new ProductPageViewModel
            {
                Product = viewModel,
                HasEditAccess = _currentUserService.HasPermission("Product.Write")
            };
            await LoadDropdownItemAsync(pageViewModel);

            return View("Detail", pageViewModel);
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.PRODUCT_READ}")]
        [HttpGet]
        public async Task<IActionResult> View(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("View product accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Viewing product {ProductId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Product.Write");

            // Check if user has read permission
            if (!hasEditAccess && !_currentUserService.HasPermission("Product.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to view product {ProductId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var product = await _productService.GetProductByIdAsync(id.Value);

                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found when accessed by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new ProductPageViewModel
                {
                    Product = _mapper.Map<ProductViewModel>(product),
                    HasEditAccess = false, // View mode
                    IsEdit = true
                };

                await LoadDropdownItemAsync(viewModel);

                _logger.LogDebug("Successfully loaded product {ProductId} for viewing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product {ProductId} for user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load product details");
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.PRODUCT_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("Edit product accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Editing product {ProductId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Product.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("Product.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit product {ProductId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var product = await _productService.GetProductByIdAsync(id.Value);

                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found when accessed for editing by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new ProductPageViewModel
                {
                    Product = _mapper.Map<ProductViewModel>(product),
                    HasEditAccess = hasEditAccess,
                    IsEdit = true
                };

                await LoadDropdownItemAsync(viewModel);

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product {ProductId} for editing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load product details");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            _logger.LogInformation("Updating product {ProductId} - {ProductName} by user {UserId}",
                model.Id, model.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    bool hasEditAccess = _currentUserService.HasPermission("Product.Write");

                    if (!hasEditAccess)
                    {
                        _logger.LogWarning("User {UserId} denied access to update product {ProductId} - insufficient permissions",
                            _currentUserService.UserId, model.Id);
                        return Forbid();
                    }

                    var productUpdateDto = new ProductUpdateDto
                    {
                        Name = model.Name,
                        SKU = model.SKU,
                        Barcode = model.Barcode,
                        Description = model.Description,
                        Weight = model.Weight,
                        Length = model.Length,
                        Width = model.Width,
                        Height = model.Height,
                        //UnitOfMeasure = model.UnitOfMeasure,
                        RequiresLotTracking = model.RequiresLotTracking,
                        RequiresExpirationDate = model.RequiresExpirationDate,
                        RequiresSerialNumber = model.RequiresSerialNumber,
                        //MinStockLevel = model.MinStockLevel,
                        //MaxStockLevel = model.MaxStockLevel,
                        //ReorderPoint = model.ReorderPoint,
                        //ReorderQuantity = model.ReorderQuantity,
                        //Category = model.Category,
                        SubCategory = model.SubCategory,
                        IsActive = model.IsActive,
                        IsHazardous = model.IsHazardous,
                        IsFragile = model.IsFragile,
                        ProductTypeId = model.ProductTypeId,
                        ProductCategoryId = model.ProductCategoryId,
                        UnitOfMeasureCodeId = model.UnitOfMeasureCodeId
                        //ProductImage = model.ProductImage,
                        //RemoveProductImage = model.RemoveProductImage
                    };

                    await _productService.UpdateProductAsync(model.Id, productUpdateDto);

                    _logger.LogInformation("Product updated successfully: {ProductId} - {ProductName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    _toastService.AddSuccessToast("Product updated successfully!");

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    return RedirectToAction("View", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product {ProductId} - {ProductName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error updating Product: {ex.Message}");
                    _toastService.AddErrorToast($"{ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                }
            }
            else if (Request.IsAjaxRequest())
            {
                _logger.LogWarning("Invalid model state when updating product {ProductId} by user {UserId}",
                    model.Id, _currentUserService.UserId);
                return BadRequest(new { success = false, errors = ModelState });
            }

            var viewModel = new ProductPageViewModel
            {
                Product = model,
                HasEditAccess = _currentUserService.HasPermission("Product.Write"),
                IsEdit = true
            };
            await LoadDropdownItemAsync(viewModel);

            return View("Detail", viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid productId)
        {
            _logger.LogInformation("Deleting product {ProductId} by user {UserId}", productId, _currentUserService.UserId);

            try
            {
                await _productService.DeleteProductAsync(productId);

                _logger.LogInformation("Product deleted successfully: {ProductId} by user {UserId}",
                    productId, _currentUserService.UserId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Product deleted successfully" });
                }

                _toastService.AddSuccessToast("Product deleted successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId} by user {UserId}",
                    productId, _currentUserService.UserId);

                var errorMessage = $"Failed to delete product: {ex.Message}";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                _toastService.AddErrorToast(errorMessage);
                return RedirectToAction(nameof(Index));
            }
        }

        #region Import/Export Actions

        /// <summary>
        /// Download product import template
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.PRODUCT_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("Downloading product template by user {UserId}", _currentUserService.UserId);

            try
            {
                List<string> codeTypes = new List<string>
                {
                    AppConsts.GeneralCodeType.PRODUCT_TYPE,
                    AppConsts.GeneralCodeType.PRODUCT_CATEGORY,
                    AppConsts.GeneralCodeType.PRODUCT_UOM,
                };
                var generalCodes = await _generalCodeService.GetCodesByTypesAsync(codeTypes);

                var productTypes = generalCodes.Where(x => x.GeneralCodeTypeName == AppConsts.GeneralCodeType.PRODUCT_TYPE).ToList();
                var productCategories = generalCodes.Where(x => x.GeneralCodeTypeName == AppConsts.GeneralCodeType.PRODUCT_CATEGORY).ToList();
                var productUnitOfMeasures = generalCodes.Where(x => x.GeneralCodeTypeName == AppConsts.GeneralCodeType.PRODUCT_UOM).ToList();


                var templateBytes = _productService.GenerateProductTemplate(productTypes, productCategories, productUnitOfMeasures);
                var fileName = $"Product_Import_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating product template by user {UserId}", _currentUserService.UserId);
                _toastService.AddErrorToast("Failed to generate template");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Export existing products
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.PRODUCT_READ}")]
        [HttpGet]
        public async Task<IActionResult> ExportProducts(Guid? clientId = null)
        {
            _logger.LogInformation("Exporting products by user {UserId}, ClientId: {ClientId}",
                _currentUserService.UserId, clientId);

            try
            {
                var exportBytes = await _productService.ExportProductsAsync(clientId);
                var fileName = $"Products_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(exportBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products by user {UserId}", _currentUserService.UserId);
                _toastService.AddErrorToast("Failed to export products");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Show import products page
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.PRODUCT_WRITE}")]
        [HttpGet]
        public IActionResult Import()
        {
            _logger.LogInformation("Accessing product import page by user {UserId}", _currentUserService.UserId);

            if (!_currentUserService.HasPermission("Product.Write"))
            {
                _logger.LogWarning("User {UserId} denied access to product import - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }

            return View();
        }

        /// <summary>
        /// Validate import file without saving
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ValidateImport(IFormFile file)
        {
            _logger.LogInformation("Validating product import file by user {UserId}", _currentUserService.UserId);

            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Please select a file to upload." });
                }

                var validationResult = await _productService.ValidateProductImportAsync(file);

                return Json(new
                {
                    success = validationResult.IsValid,
                    totalRows = validationResult.TotalRows,
                    validItems = validationResult.ValidItems.Count,
                    errors = validationResult.Errors,
                    warnings = validationResult.Warnings,
                    processedRows = validationResult.ValidItems.Count == validationResult.TotalRows ? validationResult.TotalRows : 0,
                    successCount = validationResult.ValidItems.Count == validationResult.TotalRows ? validationResult.TotalRows : 0,
                    errorCount = validationResult.TotalRows - validationResult.ValidItems.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating product import file by user {UserId}", _currentUserService.UserId);
                return Json(new { success = false, message = $"Validation failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Process product import
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcessImport(IFormFile file)
        {
            _logger.LogInformation("Processing product import by user {UserId}", _currentUserService.UserId);

            try
            {
                if (!_currentUserService.HasPermission("Product.Write"))
                {
                    return Json(new { success = false, message = "You don't have permission to import products." });
                }

                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Please select a file to upload." });
                }

                var importResult = await _productService.ImportProductsAsync(file);

                if (importResult.Success)
                {
                    _logger.LogInformation("Product import completed successfully by user {UserId}. Success: {SuccessCount}, Errors: {ErrorCount}",
                        _currentUserService.UserId, importResult.SuccessCount, importResult.ErrorCount);

                    _toastService.AddSuccessToast($"Import completed! {importResult.SuccessCount} products created successfully.");
                }
                else
                {
                    _logger.LogWarning("Product import completed with errors by user {UserId}. Success: {SuccessCount}, Errors: {ErrorCount}",
                        _currentUserService.UserId, importResult.SuccessCount, importResult.ErrorCount);
                }

                return Json(new
                {
                    success = importResult.Success,
                    totalRows = importResult.TotalRows,
                    processedRows = importResult.ProcessedRows,
                    successCount = importResult.SuccessCount,
                    errorCount = importResult.ErrorCount,
                    errors = importResult.Errors,
                    warnings = importResult.Warnings,
                    results = importResult.Results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing product import by user {UserId}", _currentUserService.UserId);
                return Json(new { success = false, message = $"Import failed: {ex.Message}" });
            }
        }

        #endregion

        #region API Actions for Mobile/External Access

        /// <summary>
        /// Get products by client for API access
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProductsByClient(Guid clientId)
        {
            try
            {
                var products = await _productService.GetProductsByClientIdAsync(clientId);
                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by client {ClientId}", clientId);
                return Json(new { success = false, message = "Failed to load products" });
            }
        }

        /// <summary>
        /// Search products
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term, Guid? clientId = null)
        {
            try
            {
                var products = await _productService.SearchProductsAsync(term, clientId);
                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with term {Term}", term);
                return Json(new { success = false, message = "Search failed" });
            }
        }

        /// <summary>
        /// Get low stock products
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLowStockProducts(Guid? clientId = null)
        {
            try
            {
                var products = await _productService.GetLowStockProductsAsync(clientId);
                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock products for client {ClientId}", clientId);
                return Json(new { success = false, message = "Failed to load low stock products" });
            }
        }

        #endregion

        private async Task LoadDropdownItemAsync(ProductPageViewModel viewModel)
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                viewModel.Clients = clients
                    .Where(c => c.IsActive)
                    .Select(c => new ClientDropdownItem
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Code = c.Code ?? string.Empty
                    })
                    .OrderBy(c => c.Name)
                    .ToList();

                List<string> codeTypes = new List<string>
                {
                    AppConsts.GeneralCodeType.PRODUCT_TYPE,
                    AppConsts.GeneralCodeType.PRODUCT_CATEGORY,
                    AppConsts.GeneralCodeType.PRODUCT_UOM,
                };
                var generalCodes = await _generalCodeService.GetCodesByTypesAsync(codeTypes);

                var productTypes = generalCodes.Where(x => x.GeneralCodeTypeName == AppConsts.GeneralCodeType.PRODUCT_TYPE);
                viewModel.ProductTypes = productTypes
                    .Select(c => new GeneralCodeDropDownItem
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Detail = c.Detail ?? string.Empty,
                        Sequence = c.Sequence
                    })
                    .OrderBy(c => c.Sequence)
                    .ToList();

                var productCategories = generalCodes.Where(x => x.GeneralCodeTypeName == AppConsts.GeneralCodeType.PRODUCT_CATEGORY);
                viewModel.ProductCategories = productCategories
                    .Select(c => new GeneralCodeDropDownItem
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Detail = c.Detail ?? string.Empty,
                        Sequence = c.Sequence
                    })
                    .OrderBy(c => c.Sequence)
                    .ToList();

                var productUnitOfMeasures = generalCodes.Where(x => x.GeneralCodeTypeName == AppConsts.GeneralCodeType.PRODUCT_UOM);
                viewModel.ProductUnitOfMeasures = productUnitOfMeasures
                    .Select(c => new GeneralCodeDropDownItem
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Detail = c.Detail ?? string.Empty,
                        Sequence = c.Sequence
                    })
                    .OrderBy(c => c.Sequence)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clients for product form");
                viewModel.Clients = new List<ClientDropdownItem>();
            }
        }
    }
}