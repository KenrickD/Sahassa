using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Warehouses;
using WMS.Domain.Interfaces;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.Warehouses;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class WarehouseController : Controller
    {
        private readonly IWarehouseService _warehouseService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        private readonly ILogger<WarehouseController> _logger;

        public WarehouseController(
            IWarehouseService warehouseService,
            ICurrentUserService currentUserService,
            ToastService toastService,
            IMapper mapper,
            ILogger<WarehouseController> logger)
        {
            _warehouseService = warehouseService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _mapper = mapper;
            _logger = logger;
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.WAREHOUSE_READ}")]
        public IActionResult Index()
        {
            _logger.LogInformation("Warehouse index page accessed by user {UserId}", _currentUserService.UserId);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetWarehouses(DataTablesRequest request)
        {
            _logger.LogDebug("Getting warehouses: SearchTerm={SearchTerm}, Start={Start}, Length={Length}",
                request.Search?.Value, request.Start, request.Length);

            try
            {
                var result = await _warehouseService.GetPaginatedWarehousesAsync(
                    request.Search?.Value,
                    request.Start,
                    request.Length,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc"
                );

                _logger.LogDebug("Retrieved {Count} warehouses out of {Total}", result.Items.Count, result.TotalCount);

                result.Items.ForEach(x =>
                {
                    x.HasWriteAccess = _currentUserService.HasPermission(AppConsts.Permissions.WAREHOUSE_WRITE);
                    x.HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.WAREHOUSE_DELETE);
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
                _logger.LogError(ex, "Error retrieving warehouses");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load warehouses"
                });
            }
        }

        /// <summary>
        /// Display the create Warehouse form
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.WAREHOUSE_WRITE}")]
        [HttpGet]
        public IActionResult Create()
        {
            _logger.LogInformation("Creating new warehouse - accessed by user {UserId}", _currentUserService.UserId);

            var viewModel = new WarehousePageViewModel
            {
                Warehouse = new WarehouseFormViewModel(),
                HasEditAccess = _currentUserService.HasPermission("Warehouse.Write"),
                IsEdit = false
            };

            return View("Detail", viewModel);
        }

        /// <summary>
        /// Process the create Warehouse form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WarehouseFormViewModel model)
        {
            _logger.LogInformation("Creating warehouse: {WarehouseName} by user {UserId}",
                model.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to DTO
                    var warehouseCreateDto = new WarehouseCreateDto
                    {
                        Name = model.Name,
                        Code = model.Code,
                        Address = model.Address,
                        City = model.City,
                        State = model.State,
                        Country = model.Country,
                        ZipCode = model.ZipCode,
                        ContactPerson = model.ContactPerson,
                        ContactEmail = model.ContactEmail,
                        ContactPhone = model.ContactPhone,
                        IsActive = model.IsActive,
                        // Configuration
                        RequiresLotTracking = model.RequiresLotTracking,
                        RequiresExpirationDates = model.RequiresExpirationDates,
                        UsesSerialNumbers = model.UsesSerialNumbers,
                        AutoAssignLocations = model.AutoAssignLocations,
                        InventoryStrategy = model.InventoryStrategy,
                        DefaultMeasurementUnit = model.DefaultMeasurementUnit,
                        DefaultDaysToExpiry = model.DefaultDaysToExpiry,
                        BarcodeFormat = model.BarcodeFormat,
                        CompanyLogoUrl = model.CompanyLogoUrl,
                        ThemePrimaryColor = model.ThemePrimaryColor,
                        ThemeSecondaryColor = model.ThemeSecondaryColor
                    };

                    // Create Warehouse
                    var createdWarehouse = await _warehouseService.CreateWarehouseAsync(warehouseCreateDto);

                    _logger.LogInformation("Warehouse created successfully: {WarehouseId} - {WarehouseName} by user {UserId}",
                        createdWarehouse.Id, createdWarehouse.Name, _currentUserService.UserId);


                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    _toastService.AddSuccessToast("Warehouse created successfully!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating warehouse: {WarehouseName} by user {UserId}",
                        model.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error creating Warehouse: {ex.Message}");

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
                _logger.LogWarning("Invalid model state when creating warehouse by user {UserId}", _currentUserService.UserId);
            }

            var pageViewModel = new WarehousePageViewModel
            {
                Warehouse = model,
                HasEditAccess = _currentUserService.HasPermission("Warehouse.Write"),
                IsEdit = false
            };

            return View("Detail", pageViewModel);
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.WAREHOUSE_READ}")]
        [HttpGet]
        public async Task<IActionResult> View(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("View warehouse accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Viewing warehouse {WarehouseId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Warehouse.Write");

            // Check if user has read permission
            if (!hasEditAccess && !_currentUserService.HasPermission("Warehouse.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to view warehouse {WarehouseId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var warehouse = await _warehouseService.GetWarehouseByIdAsync(id.Value);

                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse {WarehouseId} not found when accessed by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new WarehousePageViewModel
                {
                    Warehouse = new WarehouseFormViewModel
                    {
                        Id = warehouse.Id,
                        Name = warehouse.Name,
                        Code = warehouse.Code,
                        Address = warehouse.Address,
                        City = warehouse.City,
                        State = warehouse.State,
                        Country = warehouse.Country,
                        ZipCode = warehouse.ZipCode,
                        ContactPerson = warehouse.ContactPerson,
                        ContactEmail = warehouse.ContactEmail,
                        ContactPhone = warehouse.ContactPhone,
                        IsActive = warehouse.IsActive,
                        ClientCount = warehouse.ClientCount,
                        ZoneCount = warehouse.ZoneCount,
                        LocationCount = warehouse.LocationCount,
                        UserCount = warehouse.UserCount,
                        CreatedAt = warehouse.CreatedAt,
                        CreatedBy = warehouse.CreatedBy,
                        ModifiedAt = warehouse.ModifiedAt,
                        ModifiedBy = warehouse.ModifiedBy,
                        // Configuration
                        RequiresLotTracking = warehouse.Configuration?.RequiresLotTracking ?? false,
                        RequiresExpirationDates = warehouse.Configuration?.RequiresExpirationDates ?? false,
                        UsesSerialNumbers = warehouse.Configuration?.UsesSerialNumbers ?? false,
                        AutoAssignLocations = warehouse.Configuration?.AutoAssignLocations ?? false,
                        InventoryStrategy = warehouse.Configuration?.InventoryStrategy ?? Domain.Models.InventoryStrategy.FIFO,
                        DefaultMeasurementUnit = warehouse.Configuration?.DefaultMeasurementUnit,
                        DefaultDaysToExpiry = warehouse.Configuration?.DefaultDaysToExpiry ?? 365,
                        BarcodeFormat = warehouse.Configuration?.BarcodeFormat,
                        CompanyLogoUrl = warehouse.Configuration?.CompanyLogoUrl,
                        ThemePrimaryColor = warehouse.Configuration?.ThemePrimaryColor,
                        ThemeSecondaryColor = warehouse.Configuration?.ThemeSecondaryColor
                    },
                    HasEditAccess = false, // View mode
                    IsEdit = true
                };

                _logger.LogDebug("Successfully loaded warehouse {WarehouseId} for viewing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouse {WarehouseId} for user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load warehouse details");
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.WAREHOUSE_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("Edit warehouse accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Editing warehouse {WarehouseId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Warehouse.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("Warehouse.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit warehouse {WarehouseId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var warehouse = await _warehouseService.GetWarehouseByIdAsync(id.Value);

                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse {WarehouseId} not found when accessed for editing by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new WarehousePageViewModel
                {
                    Warehouse = new WarehouseFormViewModel
                    {
                        Id = warehouse.Id,
                        Name = warehouse.Name,
                        Code = warehouse.Code,
                        Address = warehouse.Address,
                        City = warehouse.City,
                        State = warehouse.State,
                        Country = warehouse.Country,
                        ZipCode = warehouse.ZipCode,
                        ContactPerson = warehouse.ContactPerson,
                        ContactEmail = warehouse.ContactEmail,
                        ContactPhone = warehouse.ContactPhone,
                        IsActive = warehouse.IsActive,
                        ClientCount = warehouse.ClientCount,
                        ZoneCount = warehouse.ZoneCount,
                        LocationCount = warehouse.LocationCount,
                        UserCount = warehouse.UserCount,
                        CreatedAt = warehouse.CreatedAt,
                        CreatedBy = warehouse.CreatedBy,
                        ModifiedAt = warehouse.ModifiedAt,
                        ModifiedBy = warehouse.ModifiedBy,
                        // Configuration
                        RequiresLotTracking = warehouse.Configuration?.RequiresLotTracking ?? false,
                        RequiresExpirationDates = warehouse.Configuration?.RequiresExpirationDates ?? false,
                        UsesSerialNumbers = warehouse.Configuration?.UsesSerialNumbers ?? false,
                        AutoAssignLocations = warehouse.Configuration?.AutoAssignLocations ?? false,
                        InventoryStrategy = warehouse.Configuration?.InventoryStrategy ?? Domain.Models.InventoryStrategy.FIFO,
                        DefaultMeasurementUnit = warehouse.Configuration?.DefaultMeasurementUnit,
                        DefaultDaysToExpiry = warehouse.Configuration?.DefaultDaysToExpiry ?? 365,
                        BarcodeFormat = warehouse.Configuration?.BarcodeFormat,
                        CompanyLogoUrl = warehouse.Configuration?.CompanyLogoUrl,
                        ThemePrimaryColor = warehouse.Configuration?.ThemePrimaryColor,
                        ThemeSecondaryColor = warehouse.Configuration?.ThemeSecondaryColor
                    },
                    HasEditAccess = hasEditAccess,
                    IsEdit = true
                };

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouse {WarehouseId} for editing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load warehouse details");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WarehouseFormViewModel model)
        {
            _logger.LogInformation("Updating warehouse {WarehouseId} - {WarehouseName} by user {UserId}",
                model.Id, model.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    bool hasEditAccess = _currentUserService.HasPermission("Warehouse.Write");

                    if (!hasEditAccess)
                    {
                        _logger.LogWarning("User {UserId} denied access to update warehouse {WarehouseId} - insufficient permissions",
                            _currentUserService.UserId, model.Id);
                        return Forbid();
                    }

                    var warehouseUpdateDto = new WarehouseUpdateDto
                    {
                        Name = model.Name,
                        Code = model.Code,
                        Address = model.Address,
                        City = model.City,
                        State = model.State,
                        Country = model.Country,
                        ZipCode = model.ZipCode,
                        ContactPerson = model.ContactPerson,
                        ContactEmail = model.ContactEmail,
                        ContactPhone = model.ContactPhone,
                        IsActive = model.IsActive
                    };

                    await _warehouseService.UpdateWarehouseAsync(model.Id, warehouseUpdateDto);

                    // Update configuration if provided
                    var configUpdateDto = new WarehouseConfigurationUpdateDto
                    {
                        RequiresLotTracking = model.RequiresLotTracking,
                        RequiresExpirationDates = model.RequiresExpirationDates,
                        UsesSerialNumbers = model.UsesSerialNumbers,
                        AutoAssignLocations = model.AutoAssignLocations,
                        InventoryStrategy = model.InventoryStrategy,
                        DefaultMeasurementUnit = model.DefaultMeasurementUnit,
                        DefaultDaysToExpiry = model.DefaultDaysToExpiry,
                        BarcodeFormat = model.BarcodeFormat,
                        CompanyLogoUrl = model.CompanyLogoUrl,
                        ThemePrimaryColor = model.ThemePrimaryColor,
                        ThemeSecondaryColor = model.ThemeSecondaryColor
                    };

                    await _warehouseService.UpdateWarehouseConfigurationAsync(model.Id, configUpdateDto);

                    _logger.LogInformation("Warehouse updated successfully: {WarehouseId} - {WarehouseName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    _toastService.AddSuccessToast("Warehouse updated successfully!");

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    return RedirectToAction("View", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating warehouse {WarehouseId} - {WarehouseName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error updating Warehouse: {ex.Message}");
                    _toastService.AddErrorToast($"{ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                }
            }
            else if (Request.IsAjaxRequest())
            {
                _logger.LogWarning("Invalid model state when updating warehouse {WarehouseId} by user {UserId}",
                    model.Id, _currentUserService.UserId);
                return BadRequest(new { success = false, errors = ModelState });
            }

            var viewModel = new WarehousePageViewModel
            {
                Warehouse = model,
                HasEditAccess = _currentUserService.HasPermission("Warehouse.Write"),
                IsEdit = true
            };

            return View("Detail", viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid warehouseId)
        {
            _logger.LogInformation("Deleting warehouse {WarehouseId} by user {UserId}", warehouseId, _currentUserService.UserId);

            if (!_currentUserService.HasPermission("Warehouse.Delete"))
            {
                _logger.LogWarning("User {UserId} denied access to delete warehouse {WarehouseId} - insufficient permissions",
                    _currentUserService.UserId, warehouseId);
                return Forbid();
            }

            try
            {
                await _warehouseService.DeleteWarehouseAsync(warehouseId);

                _logger.LogInformation("Warehouse deleted successfully: {WarehouseId} by user {UserId}",
                    warehouseId, _currentUserService.UserId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Warehouse deleted successfully" });
                }

                _toastService.AddSuccessToast("Warehouse deleted successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting warehouse {WarehouseId} by user {UserId}",
                    warehouseId, _currentUserService.UserId);

                var errorMessage = $"Failed to delete warehouse: {ex.Message}";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                _toastService.AddErrorToast(errorMessage);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid warehouseId, bool isActive)
        {
            _logger.LogInformation("Toggling warehouse status: {WarehouseId} to {IsActive} by user {UserId}",
                warehouseId, isActive, _currentUserService.UserId);

            if (!_currentUserService.HasPermission("Warehouse.Write"))
            {
                return Json(new { success = false, message = "You don't have permission to modify warehouses" });
            }

            try
            {
                await _warehouseService.ActivateWarehouseAsync(warehouseId, isActive);

                var statusText = isActive ? "activated" : "deactivated";
                _logger.LogInformation("Warehouse {StatusText} successfully: {WarehouseId} by user {UserId}",
                    statusText, warehouseId, _currentUserService.UserId);

                return Json(new { success = true, message = $"Warehouse {statusText} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling warehouse status {WarehouseId} by user {UserId}",
                    warehouseId, _currentUserService.UserId);

                return Json(new { success = false, message = $"Failed to update warehouse status: {ex.Message}" });
            }
        }

        #region API Methods for Dropdowns

        [HttpGet]
        public async Task<IActionResult> GetWarehousesForDropdown()
        {
            try
            {
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                var warehouseList = warehouses
                    .Where(w => w.IsActive)
                    .Select(w => new {
                        id = w.Id,
                        name = w.Name,
                        code = w.Code,
                        displayText = !string.IsNullOrEmpty(w.Code) ? $"{w.Name} ({w.Code})" : w.Name
                    })
                    .OrderBy(w => w.name)
                    .ToList();

                return Json(new { success = true, data = warehouseList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouses for dropdown");
                return Json(new { success = false, message = "Failed to load warehouses" });
            }
        }

        #endregion
    }
}