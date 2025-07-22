using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Extensions;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Zones;
using WMS.Domain.Interfaces;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.Clients;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.Zones;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class ZoneController : Controller
    {
        private readonly IZoneService _zoneService;
        private readonly IWarehouseService _warehouseService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        private readonly ILogger<ZoneController> _logger;

        public ZoneController(
            IZoneService zoneService,
            IWarehouseService warehouseService,
            ICurrentUserService currentUserService,
            ToastService toastService,
            IMapper mapper,
            ILogger<ZoneController> logger)
        {
            _zoneService = zoneService;
            _warehouseService = warehouseService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _mapper = mapper;
            _logger = logger;
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.ZONE_READ}")]
        public IActionResult Index()
        {
            _logger.LogInformation("Zone index page accessed by user {UserId}", _currentUserService.UserId);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetZones(DataTablesRequest request)
        {
            _logger.LogDebug("Getting zones: SearchTerm={SearchTerm}, Start={Start}, Length={Length}",
                request.Search?.Value, request.Start, request.Length);

            try
            {
                var result = await _zoneService.GetPaginatedZones(
                    request.Search?.Value,
                    request.Start,
                    request.Length,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc"
                );

                _logger.LogDebug("Retrieved {Count} zones out of {Total}", result.Items.Count, result.TotalCount);

                result.Items.ForEach(x =>
                {
                    x.HasWriteAccess = _currentUserService.HasPermission(AppConsts.Permissions.ZONE_WRITE);
                    x.HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.ZONE_DELETE);
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
                _logger.LogError(ex, "Error retrieving zones");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load zones"
                });
            }
        }

        /// <summary>
        /// Display the create Zone form
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.ZONE_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Creating new zone - accessed by user {UserId}", _currentUserService.UserId);

            var viewModel = new ZonePageViewModel();
            viewModel.HasEditAccess = _currentUserService.HasPermission("Zone.Write");

            // Load warehouses for dropdown
            await LoadWarehousesAsync(viewModel);

            return View("Detail", viewModel);
        }

        /// <summary>
        /// Process the create Zone form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ZoneViewModel viewModel)
        {
            _logger.LogInformation("Creating zone: {ZoneName} by user {UserId}",
                viewModel.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to DTO
                    var zoneCreateDto = new ZoneCreateDto
                    {
                        Name = viewModel.Name,
                        Code = viewModel.Code,
                        Description = viewModel.Description,
                        IsActive = viewModel.IsActive,
                        WarehouseId = viewModel.WarehouseId
                    };

                    // Create Zone
                    var createdZone = await _zoneService.CreateZoneAsync(zoneCreateDto);

                    _logger.LogInformation("Zone created successfully: {ZoneId} - {ZoneName} by user {UserId}",
                        createdZone.Id, createdZone.Name, _currentUserService.UserId);


                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    _toastService.AddSuccessToast("Zone created successfully!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating zone: {ZoneName} by user {UserId}",
                        viewModel.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error creating Zone: {ex.Message}");

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
                _logger.LogWarning("Invalid model state when creating zone by user {UserId}", _currentUserService.UserId);
            }

            var pageViewModel = new ZonePageViewModel
            {
                Zone = viewModel,
                HasEditAccess = _currentUserService.HasPermission("Zone.Write")
            };
            await LoadWarehousesAsync(pageViewModel);

            return View("Detail", pageViewModel);
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.ZONE_READ}")]
        [HttpGet]
        public async Task<IActionResult> View(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("View zone accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Viewing zone {ZoneId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Zone.Write");

            // Check if user has read permission
            if (!hasEditAccess && !_currentUserService.HasPermission("Zone.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to view zone {ZoneId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var zone = await _zoneService.GetZoneByIdAsync(id.Value);

                if (zone == null)
                {
                    _logger.LogWarning("Zone {ZoneId} not found when accessed by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new ZonePageViewModel
                {
                    Zone = new ZoneViewModel
                    {
                        Id = zone.Id,
                        Name = zone.Name,
                        Code = zone.Code,
                        Description = zone.Description,
                        IsActive = zone.IsActive,
                        WarehouseId = zone.WarehouseId,
                        WarehouseName = zone.WarehouseName,
                        LocationCount = zone.LocationCount
                    },
                    HasEditAccess = false, // View mode
                    IsEdit = true
                };

                await LoadWarehousesAsync(viewModel);

                _logger.LogDebug("Successfully loaded zone {ZoneId} for viewing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading zone {ZoneId} for user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load zone details");
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.ZONE_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("Edit zone accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Editing zone {ZoneId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Zone.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("Zone.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit zone {ZoneId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var zone = await _zoneService.GetZoneByIdAsync(id.Value);

                if (zone == null)
                {
                    _logger.LogWarning("Zone {ZoneId} not found when accessed for editing by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new ZonePageViewModel
                {
                    Zone = new ZoneViewModel
                    {
                        Id = zone.Id,
                        Name = zone.Name,
                        Code = zone.Code,
                        Description = zone.Description,
                        IsActive = zone.IsActive,
                        WarehouseId = zone.WarehouseId,
                        WarehouseName = zone.WarehouseName,
                        LocationCount = zone.LocationCount,
                        CreatedAt = DateTimeExtensions.AsSingaporeTime(zone.CreatedAt)
                    },
                    HasEditAccess = hasEditAccess,
                    IsEdit = true
                };

                await LoadWarehousesAsync(viewModel);

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading zone {ZoneId} for editing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load zone details");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ZoneViewModel model)
        {
            _logger.LogInformation("Updating zone {ZoneId} - {ZoneName} by user {UserId}",
                model.Id, model.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    bool hasEditAccess = _currentUserService.HasPermission("Zone.Write");

                    if (!hasEditAccess)
                    {
                        _logger.LogWarning("User {UserId} denied access to update zone {ZoneId} - insufficient permissions",
                            _currentUserService.UserId, model.Id);
                        return Forbid();
                    }

                    var zoneUpdateDto = new ZoneUpdateDto
                    {
                        Name = model.Name,
                        Code = model.Code,
                        Description = model.Description,
                        IsActive = model.IsActive
                    };

                    await _zoneService.UpdateZoneAsync(model.Id, zoneUpdateDto);

                    _logger.LogInformation("Zone updated successfully: {ZoneId} - {ZoneName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    _toastService.AddSuccessToast("Zone updated successfully!");

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    return RedirectToAction("View", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating zone {ZoneId} - {ZoneName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error updating Zone: {ex.Message}");
                    _toastService.AddErrorToast($"{ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                }
            }
            else if (Request.IsAjaxRequest())
            {
                _logger.LogWarning("Invalid model state when updating zone {ZoneId} by user {UserId}",
                    model.Id, _currentUserService.UserId);
                return BadRequest(new { success = false, errors = ModelState });
            }

            var viewModel = new ZonePageViewModel
            {
                Zone = model,
                HasEditAccess = _currentUserService.HasPermission("Zone.Write"),
                IsEdit = true
            };
            await LoadWarehousesAsync(viewModel);

            return View("Detail", viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid zoneId)
        {
            _logger.LogInformation("Deleting zone {ZoneId} by user {UserId}", zoneId, _currentUserService.UserId);

            try
            {
                await _zoneService.DeleteZoneAsync(zoneId);

                _logger.LogInformation("Zone deleted successfully: {ZoneId} by user {UserId}",
                    zoneId, _currentUserService.UserId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Zone deleted successfully" });
                }

                _toastService.AddSuccessToast("Zone deleted successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting zone {ZoneId} by user {UserId}",
                    zoneId, _currentUserService.UserId);

                var errorMessage = $"Failed to delete zone: {ex.Message}";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                _toastService.AddErrorToast(errorMessage);
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task LoadWarehousesAsync(ZonePageViewModel viewModel)
        {
            try
            {
                bool isAdmin = _currentUserService.IsInRole(AppConsts.Roles.SYSTEM_ADMIN);

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouses for zone form");
                viewModel.Warehouses = new List<WarehouseDropdownItem>();
            }
        }
    }
}