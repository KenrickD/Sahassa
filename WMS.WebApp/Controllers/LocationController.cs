using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.Clients;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.Locations;
using WMS.WebApp.Models.Zones;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class LocationController : Controller
    {
        private readonly ILocationService _locationService;
        private readonly IZoneService _zoneService;
        private readonly IWarehouseService _warehouseService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        private readonly ILogger<LocationController> _logger;

        public LocationController(
            ILocationService locationService,
            IZoneService zoneService,
            IWarehouseService warehouseService,
            ICurrentUserService currentUserService,
            ToastService toastService,
            IMapper mapper,
            ILogger<LocationController> logger)
        {
            _locationService = locationService;
            _zoneService = zoneService;
            _warehouseService = warehouseService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _mapper = mapper;
            _logger = logger;
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.LOCATION_READ}")]
        public IActionResult Index()
        {
            _logger.LogInformation("Location index page accessed by user {UserId}", _currentUserService.UserId);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetLocations(DataTablesRequest request, Guid? warehouseId = null, Guid? zoneId = null)
        {
            _logger.LogDebug("Getting locations: SearchTerm={SearchTerm}, Start={Start}, Length={Length}, WarehouseId={WarehouseId}, ZoneId={ZoneId}",
                request.Search?.Value, request.Start, request.Length, warehouseId, zoneId);

            try
            {
                var result = await _locationService.GetPaginatedLocations(
                    request.Search?.Value,
                    request.Start,
                    request.Length,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc",
                    warehouseId,
                    zoneId
                );

                _logger.LogDebug("Retrieved {Count} locations out of {Total}", result.Items.Count, result.TotalCount);

                result.Items.ForEach(x =>
                {
                    x.HasWriteAccess = _currentUserService.HasPermission(AppConsts.Permissions.LOCATION_WRITE);
                    x.HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.LOCATION_DELETE);
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
                _logger.LogError(ex, "Error retrieving locations");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load locations"
                });
            }
        }

        /// <summary>
        /// Display the create Location form
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.LOCATION_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Creating new location - accessed by user {UserId}", _currentUserService.UserId);

            var viewModel = new LocationPageViewModel();
            viewModel.HasEditAccess = _currentUserService.HasPermission("Location.Write");

            // Load zones for dropdown
            await LoadZonesAsync(viewModel);

            return View("Detail", viewModel);
        }

        /// <summary>
        /// Process the create Location form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LocationViewModel viewModel)
        {
            _logger.LogInformation("Creating location: {LocationName} by user {UserId}",
                viewModel.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to DTO
                    var locationCreateDto = new LocationCreateDto
                    {
                        ZoneId = viewModel.ZoneId,
                        WarehouseId = viewModel.WarehouseId,
                        Name = viewModel.Name,
                        Code = viewModel.Code,
                        //Type = viewModel.Type,
                        //AccessType = viewModel.AccessType,
                        IsActive = viewModel.IsActive,
                        MaxWeight = viewModel.MaxWeight,
                        MaxVolume = viewModel.MaxVolume,
                        MaxItems = viewModel.MaxItems,
                        Barcode = viewModel.Barcode,
                        Length = viewModel.Length,
                        Width = viewModel.Width,
                        Height = viewModel.Height,
                        Row = viewModel.Row,
                        Bay = viewModel.Bay,
                        Level = viewModel.Level,
                        Aisle = viewModel.Aisle,
                        Side = viewModel.Side,
                        Bin = viewModel.Bin,
                        //PickingPriority = viewModel.PickingPriority,
                        //TemperatureZone = viewModel.TemperatureZone,
                        XCoordinate = viewModel.XCoordinate,
                        YCoordinate = viewModel.YCoordinate,
                        ZCoordinate = viewModel.ZCoordinate
                    };

                    // Create Location
                    var createdLocation = await _locationService.CreateLocationAsync(locationCreateDto);

                    _logger.LogInformation("Location created successfully: {LocationId} - {LocationName} by user {UserId}",
                        createdLocation.Id, createdLocation.Name, _currentUserService.UserId);

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    _toastService.AddSuccessToast("Location created successfully!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating location: {LocationName} by user {UserId}",
                        viewModel.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error creating Location: {ex.Message}");

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
                _logger.LogWarning("Invalid model state when creating location by user {UserId}", _currentUserService.UserId);
            }

            var pageViewModel = new LocationPageViewModel
            {
                Location = viewModel,
                HasEditAccess = _currentUserService.HasPermission("Location.Write")
            };
            await LoadZonesAsync(pageViewModel);

            return View("Detail", pageViewModel);
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.LOCATION_READ}")]
        [HttpGet]
        public async Task<IActionResult> View(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("View location accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Viewing location {LocationId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Location.Write");

            // Check if user has read permission
            if (!hasEditAccess && !_currentUserService.HasPermission("Location.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to view location {LocationId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var location = await _locationService.GetLocationByIdAsync(id.Value);

                if (location == null)
                {
                    _logger.LogWarning("Location {LocationId} not found when accessed by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new LocationPageViewModel
                {
                    Location = _mapper.Map<LocationViewModel>(location),
                    HasEditAccess = false, // View mode
                    IsEdit = true
                };

                await LoadZonesAsync(viewModel);

                _logger.LogDebug("Successfully loaded location {LocationId} for viewing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading location {LocationId} for user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load location details");
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.LOCATION_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("Edit location accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Editing location {LocationId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Location.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("Location.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit location {LocationId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var location = await _locationService.GetLocationByIdAsync(id.Value);

                if (location == null)
                {
                    _logger.LogWarning("Location {LocationId} not found when accessed for editing by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new LocationPageViewModel
                {
                    Location = _mapper.Map<LocationViewModel>(location),
                    HasEditAccess = hasEditAccess,
                    IsEdit = true
                };

                await LoadZonesAsync(viewModel);

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading location {LocationId} for editing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load location details");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LocationViewModel model)
        {
            _logger.LogInformation("Updating location {LocationId} - {LocationName} by user {UserId}",
                model.Id, model.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    bool hasEditAccess = _currentUserService.HasPermission("Location.Write");

                    if (!hasEditAccess)
                    {
                        _logger.LogWarning("User {UserId} denied access to update location {LocationId} - insufficient permissions",
                            _currentUserService.UserId, model.Id);
                        return Forbid();
                    }

                    var locationUpdateDto = _mapper.Map<LocationUpdateDto>(model);

                    await _locationService.UpdateLocationAsync(model.Id, locationUpdateDto);

                    _logger.LogInformation("Location updated successfully: {LocationId} - {LocationName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    _toastService.AddSuccessToast("Location updated successfully!");

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    return RedirectToAction("View", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating location {LocationId} - {LocationName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error updating Location: {ex.Message}");
                    _toastService.AddErrorToast($"{ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                }
            }
            else if (Request.IsAjaxRequest())
            {
                _logger.LogWarning("Invalid model state when updating location {LocationId} by user {UserId}",
                    model.Id, _currentUserService.UserId);
                return BadRequest(new { success = false, errors = ModelState });
            }

            var viewModel = new LocationPageViewModel
            {
                Location = model,
                HasEditAccess = _currentUserService.HasPermission("Location.Write"),
                IsEdit = true
            };
            await LoadZonesAsync(viewModel);

            return View("Detail", viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid locationId)
        {
            _logger.LogInformation("Deleting location {LocationId} by user {UserId}", locationId, _currentUserService.UserId);

            try
            {
                await _locationService.DeleteLocationAsync(locationId);

                _logger.LogInformation("Location deleted successfully: {LocationId} by user {UserId}",
                    locationId, _currentUserService.UserId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Location deleted successfully" });
                }

                _toastService.AddSuccessToast("Location deleted successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting location {LocationId} by user {UserId}",
                    locationId, _currentUserService.UserId);

                var errorMessage = $"Failed to delete location: {ex.Message}";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                _toastService.AddErrorToast(errorMessage);
                return RedirectToAction(nameof(Index));
            }
        }

        #region Barcodes
        [HttpGet]
        public async Task<IActionResult> Barcode()
        {
            _logger.LogInformation("Accessing location barcode page by user {UserId}", _currentUserService.UserId);

            if (!_currentUserService.HasPermission("Location.Write"))
            {
                _logger.LogWarning("User {UserId} denied access to location barcode - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }

            var viewModel = new LocationBarcodeViewModel();
            bool isAdmin = _currentUserService.IsInRole(AppConsts.Roles.SYSTEM_ADMIN);

            // Load warehouses
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            viewModel.Warehouses = warehouses
                .Where(w => w.IsActive && (w.Id == _currentUserService.CurrentWarehouseId || isAdmin))
                .Select(w => new WarehouseDropdownItem
                {
                    Id = w.Id,
                    Name = w.Name,
                    Code = w.Code
                })
                .OrderBy(w => w.Name)
                .ToList();

            // Load zones (all zones, will be filtered by warehouse on client-side)
            var zones = await _zoneService.GetAllZonesAsync();
            viewModel.Zones = zones
                .Where(z => z.IsActive)
                .Select(z => new ZoneDropdownItem
                {
                    Id = z.Id,
                    Name = z.Name,
                    Code = z.Code,
                    WarehouseId = z.WarehouseId,
                    WarehouseName = z.WarehouseName
                })
                .OrderBy(z => z.WarehouseName)
                .ThenBy(z => z.Name)
                .ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> GenerateBarcode(LocationBarcodeViewModel viewModel)
        {
            _logger.LogInformation("Generate barcode by user {UserId}", _currentUserService.UserId);
            try
            {
                var zoneId = viewModel.ZoneId;
                if (zoneId != null)
                {
                    var locations = await _locationService.GetLocationsByZoneIdAsync(zoneId.Value);
                    List<string> locationBarcodes = new List<string>();
                    foreach (var location in locations)
                    {
                        //only take not null location barcode
                        if (location.Barcode != null)
                        {
                            locationBarcodes.Add(location.Barcode);
                        }
                    }
                    var pdfHelper = new PDFHelper();
                    PdfDocument barcodes = pdfHelper.GenerateBarcodesInPDF(locationBarcodes);
                    MemoryStream stream = new MemoryStream();
                    barcodes.Save(stream);

                    Response.ContentType = "application/pdf";
                    Response.Headers.Add("content-length", stream.Length.ToString());
                    byte[] bytes = stream.ToArray();
                    stream.Close();

                    var zoneName = (await _zoneService.GetZoneByIdAsync(zoneId.Value)).Name;
                    var warehouseId = (await _zoneService.GetZoneByIdAsync(zoneId.Value)).WarehouseId;
                    var warehouseName = (await _warehouseService.GetWarehouseByIdAsync(warehouseId)).Name;

                    return File(bytes, "application/pdf", $"{warehouseName} {zoneName} - location barcodes" + ".pdf");
                }
                else
                {
                    throw new Exception("Zone cannot be null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating barcode by user {UserId}", _currentUserService.UserId);

                var errorMessage = $"Failed to generate barcode: {ex.Message}";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                _toastService.AddErrorToast(errorMessage);
                return RedirectToAction(nameof(Barcode));
            }
        }

        #endregion

        #region Import/Export Actions

        /// <summary>
        /// Download location import template
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.LOCATION_WRITE}")]
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            _logger.LogInformation("Downloading location template by user {UserId}", _currentUserService.UserId);

            try
            {
                var templateBytes = _locationService.GenerateLocationTemplate();
                var fileName = $"Location_Import_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating location template by user {UserId}", _currentUserService.UserId);
                _toastService.AddErrorToast("Failed to generate template");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Export existing locations
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.LOCATION_READ}")]
        [HttpGet]
        public async Task<IActionResult> ExportLocations(Guid? zoneId = null)
        {
            _logger.LogInformation("Exporting locations by user {UserId}, ZoneId: {ZoneId}",
                _currentUserService.UserId, zoneId);

            try
            {
                var exportBytes = await _locationService.ExportLocationsAsync(zoneId);
                var fileName = $"Locations_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(exportBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting locations by user {UserId}", _currentUserService.UserId);
                _toastService.AddErrorToast("Failed to export locations");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Show import locations page
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.LOCATION_WRITE}")]
        [HttpGet]
        public IActionResult Import()
        {
            _logger.LogInformation("Accessing location import page by user {UserId}", _currentUserService.UserId);

            if (!_currentUserService.HasPermission("Location.Write"))
            {
                _logger.LogWarning("User {UserId} denied access to location import - insufficient permissions",
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
            _logger.LogInformation("Validating location import file by user {UserId}", _currentUserService.UserId);

            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Please select a file to upload." });
                }

                var validationResult = await _locationService.ValidateLocationImportAsync(file);

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
                _logger.LogError(ex, "Error validating location import file by user {UserId}", _currentUserService.UserId);
                return Json(new { success = false, message = $"Validation failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Process location import
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcessImport(IFormFile file)
        {
            _logger.LogInformation("Processing location import by user {UserId}", _currentUserService.UserId);

            try
            {
                if (!_currentUserService.HasPermission("Location.Write"))
                {
                    return Json(new { success = false, message = "You don't have permission to import locations." });
                }

                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Please select a file to upload." });
                }

                var importResult = await _locationService.ImportLocationsAsync(file);

                if (importResult.Success)
                {
                    _logger.LogInformation("Location import completed successfully by user {UserId}. Success: {SuccessCount}, Errors: {ErrorCount}",
                        _currentUserService.UserId, importResult.SuccessCount, importResult.ErrorCount);

                    _toastService.AddSuccessToast($"Import completed! {importResult.SuccessCount} locations created successfully.");
                }
                else
                {
                    _logger.LogWarning("Location import completed with errors by user {UserId}. Success: {SuccessCount}, Errors: {ErrorCount}",
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
                _logger.LogError(ex, "Error processing location import by user {UserId}", _currentUserService.UserId);
                return Json(new { success = false, message = $"Import failed: {ex.Message}" });
            }
        }

        #endregion

        #region API Actions for Mobile/External Access

        /// <summary>
        /// Get locations by zone for API access
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLocationsByZone(Guid zoneId)
        {
            try
            {
                var locations = await _locationService.GetLocationsByZoneIdAsync(zoneId, true); // Only active locations
                return Json(new { success = true, data = locations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting locations by zone {ZoneId}", zoneId);
                return Json(new { success = false, message = "Failed to load locations" });
            }
        }

        /// <summary>
        /// Get empty locations for allocation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEmptyLocations(Guid? zoneId = null)
        {
            try
            {
                var locations = await _locationService.GetEmptyLocationsAsync(zoneId);
                return Json(new { success = true, data = locations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting empty locations for zone {ZoneId}", zoneId);
                return Json(new { success = false, message = "Failed to load empty locations" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetZonesByWarehouse(Guid warehouseId)
        {
            try
            {
                var zones = await _zoneService.GetZonesByWarehouseIdAsync(warehouseId, true); // Only active zones
                var zoneList = zones.Select(z => new
                {
                    id = z.Id,
                    name = z.Name,
                    code = z.Code,
                    warehouseId = z.WarehouseId,
                    warehouseName = z.WarehouseName
                }).ToList();

                return Json(new { success = true, data = zoneList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting zones by warehouse {WarehouseId}", warehouseId);
                return Json(new { success = false, message = "Failed to load zones" });
            }
        }
        /// <summary>
        /// Search locations
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchLocations(string term, Guid? zoneId = null)
        {
            try
            {
                var locations = await _locationService.SearchLocationsAsync(term, zoneId);
                return Json(new { success = true, data = locations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching locations with term {Term}", term);
                return Json(new { success = false, message = "Search failed" });
            }
        }
        /// <summary>
        /// Get warehouses for dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWarehousesForDropdown()
        {
            try
            {
                bool isAdmin = _currentUserService.IsInRole(AppConsts.Roles.SYSTEM_ADMIN);
                var warehouses = await _warehouseService.GetAllWarehousesAsync();

                var warehouseList = warehouses
                    .Where(w => w.IsActive && (w.Id == _currentUserService.CurrentWarehouseId || isAdmin))
                    .Select(w => new
                    {
                        id = w.Id,
                        name = w.Name,
                        code = w.Code
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
        private async Task LoadWarehousesAndZonesAsync(LocationPageViewModel viewModel)
        {
            try
            {
                bool isAdmin = _currentUserService.IsInRole(AppConsts.Roles.SYSTEM_ADMIN);

                // Load warehouses
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                viewModel.Warehouses = warehouses
                    .Where(w => w.IsActive && (w.Id == _currentUserService.CurrentWarehouseId || isAdmin))
                    .Select(w => new WarehouseDropdownItem
                    {
                        Id = w.Id,
                        Name = w.Name,
                        Code = w.Code
                    })
                    .OrderBy(w => w.Name)
                    .ToList();

                // Load zones (all zones, will be filtered by warehouse on client-side)
                var zones = await _zoneService.GetAllZonesAsync();
                viewModel.Zones = zones
                    .Where(z => z.IsActive)
                    .Select(z => new ZoneDropdownItem
                    {
                        Id = z.Id,
                        Name = z.Name,
                        Code = z.Code,
                        WarehouseId = z.WarehouseId,
                        WarehouseName = z.WarehouseName
                    })
                    .OrderBy(z => z.WarehouseName)
                    .ThenBy(z => z.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouses and zones for location form");
                viewModel.Warehouses = new List<WarehouseDropdownItem>();
                viewModel.Zones = new List<ZoneDropdownItem>();
            }
        }

        // Replace the existing LoadZonesAsync method with this:
        private async Task LoadZonesAsync(LocationPageViewModel viewModel)
        {
            await LoadWarehousesAndZonesAsync(viewModel);
        }
    }
}