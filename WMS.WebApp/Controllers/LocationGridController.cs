using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Domain.Interfaces;
using WMS.WebApp.Models.LocationGrids;
using Microsoft.EntityFrameworkCore;
using WMS.Infrastructure.Data;
using WMS.WebApp.Models.Zones;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Locations;
using WMS.Application.Helpers;
using WMS.Domain.DTOs.LocationGrids;
using static WMS.Domain.Enumerations.Enumerations;
using Microsoft.AspNetCore.Authorization;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class LocationGridController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IWarehouseNotificationService _warehouseNotificationService;
        private readonly ILocationService _locationService;
        private readonly LocationGridHelper _locationGridHelper;
        private readonly ILogger<LocationGridController> _logger;
        private readonly PDFHelper _pdfHelper;

        public LocationGridController(
            AppDbContext context,
            ICurrentUserService currentUserService,
            IWarehouseNotificationService warehouseNotificationService,
            ILocationService locationService,
            LocationGridHelper locationGridHelper,
            PDFHelper pdfHelper,
            ILogger<LocationGridController> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _warehouseNotificationService = warehouseNotificationService;
            _locationService = locationService;
            _locationGridHelper = locationGridHelper;
            _pdfHelper = pdfHelper;
            _logger = logger;
        }
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.LOCATION_GRID_READ}")]
        public async Task<IActionResult> Index(Guid? zoneId)
        {
            _logger.LogInformation("LocationGrid accessed by user {UserId}", _currentUserService.UserId);

            var warehouseId = _currentUserService.CurrentWarehouseId;

            // Get zones for the current warehouse
            var zones = await _context.Zones
                .Where(z => z.WarehouseId == warehouseId && !z.IsDeleted && z.IsActive)
                .OrderBy(z => z.Name)
                .Select(z => new ZoneDropdownItem
                {
                    Id = z.Id,
                    Name = z.Name,
                    Code = z.Code
                })
                .ToListAsync();

            // If zoneId is provided, use it; otherwise use first zone
            var selectedZoneId = zoneId ?? zones.FirstOrDefault()?.Id ?? Guid.Empty;

            var viewModel = new LocationGridViewModel
            {
                Zones = zones,
                SelectedZoneId = selectedZoneId,
                WarehouseId = warehouseId
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationData(Guid zoneId)
        {
            try
            {
                _logger.LogDebug("Getting location data for zone {ZoneId}", zoneId);

                var zone = await _context.Zones.FindAsync(zoneId);
                if (zone == null) return NotFound();

                var locations = await _context.Locations
                     .Where(l => l.ZoneId == zoneId && !l.IsDeleted)
                     .Include(l => l.Inventories.Where(i => !i.IsDeleted))
                     .Include(l => l.GIVRMReceivePallets.Where(i => !i.IsDeleted))
                     .Include(l => l.GIVFGReceivePallets.Where(i => !i.IsDeleted))
                     .AsNoTracking()
                     .Select(l => new
                     {
                         Location = l,
                         TotalItemCount = l.Inventories.Count(i => !i.IsDeleted) +
                                         l.GIVRMReceivePallets.Count(i => !i.IsDeleted) +
                                         l.GIVFGReceivePallets.Count(i => !i.IsDeleted)
                     })
                     .Select(x => new LocationGridItem
                     {
                         Id = x.Location.Id,
                         Code = x.Location.Code,
                         Barcode = x.Location.Barcode ?? string.Empty,
                         Name = x.Location.Name,
                         Row = x.Location.Row ?? "",
                         Bay = x.Location.Bay ?? 0,
                         Level = x.Location.Level ?? 0,
                         IsEmpty = x.Location.IsEmpty,
                         Status = x.Location.IsEmpty || x.TotalItemCount == 0
                             ? LocationStatus.Available
                             : (x.TotalItemCount < x.Location.MaxItems
                                 ? LocationStatus.Partial
                                 : LocationStatus.Occupied),
                         StatusName = x.Location.IsEmpty || x.TotalItemCount == 0
                             ? AppConsts.LocationGridStatus.AVAILABLE
                             : (x.TotalItemCount < x.Location.MaxItems
                                 ? AppConsts.LocationGridStatus.PARTIAL
                                 : AppConsts.LocationGridStatus.OCCUPIED),
                         InventoryCount = x.TotalItemCount,
                         TotalQuantity = x.Location.Inventories.Where(i => !i.IsDeleted).Sum(i => i.Quantity),
                         MaxWeight = x.Location.MaxWeight,
                         MaxVolume = x.Location.MaxVolume,
                         MaxItems = x.Location.MaxItems
                     })
                     .ToListAsync();

                var gridData = new LocationGridData
                {
                    Locations = locations,
                    MaxRow = locations.Any() ?
                        locations.Where(l => !string.IsNullOrEmpty(l.Row) && l.Row.Length == 1 && l.Row[0] >= 'A' && l.Row[0] <= 'Z')
                                .Select(l => l.Row[0] - 'A' + 1) // Convert A=1, B=2, etc.
                                .DefaultIfEmpty(1)
                                .Max() : 1,
                    MaxBay = locations.Any() ? locations.Max(l => l.Bay) : 0,
                    MaxLevel = 5, // Fixed as per requirements,
                    Zone = zone == null ? null : new ZoneItemViewModel { Code = zone!.Code, Name = zone.Name, Id = zone.Id }
                };

                return Json(gridData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location data for zone {ZoneId}", zoneId);
                return Json(new { error = "Failed to load location data" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationDetails(Guid locationId)
        {
            try
            {
                var location = await _context.Locations
                    .Include(l => l.Zone)
                    .Include(l => l.Inventories.Where(i => !i.IsDeleted))
                        .ThenInclude(i => i.Product)
                    .Include(l => l.GIVFGReceivePallets.Where(i => !i.IsDeleted))
                        .ThenInclude(i => i.Receive).ThenInclude(i => i.FinishedGood)
                    .Include(l => l.GIVRMReceivePallets.Where(i => !i.IsDeleted))
                        .ThenInclude(i => i.GIV_RM_Receive).ThenInclude(i => i.RawMaterial)
                    .FirstOrDefaultAsync(l => l.Id == locationId && !l.IsDeleted);

                if (location == null)
                {
                    return NotFound();
                }

                var details = new LocationDetailsViewModel
                {
                    Id = location.Id,
                    Barcode = location.Barcode ?? "",
                    Code = location.Code,
                    Name = location.Name,
                    ZoneName = location.Zone.Name,
                    Row = location.Row ?? "",
                    Bay = location.Bay ?? 0,
                    Level = location.Level ?? 0,
                    IsEmpty = location.IsEmpty,
                    MaxWeight = location.MaxWeight,
                    MaxVolume = location.MaxVolume,
                    MaxItems = location.MaxItems,
                    CurrentWeight = location.Inventories.Sum(i => i.Product.Weight * i.Quantity),
                    CurrentVolume = location.Inventories.Sum(i => i.Product.Length * i.Product.Width * i.Product.Height * i.Quantity),
                    CurrentItems = location.Inventories.Count,
                    Inventories = location.Inventories.Select(i => new InventoryItemViewModel
                    {
                        Id = i.Id,
                        ProductName = i.Product.Name,
                        ProductSKU = i.Product.SKU,
                        LotNumber = i.LotNumber,
                        SerialNumber = i.SerialNumber,
                        Quantity = i.Quantity,
                        ExpirationDate = i.ExpirationDate,
                        ReceivedDate = i.ReceivedDate,
                        Status = i.Status.ToString(),
                        MainHU = ""
                    }).ToList()
                };
                details.Inventories.AddRange(location.GIVRMReceivePallets.Select(i => new InventoryItemViewModel
                {
                    Id = i.Id,
                    ProductName = i.GIV_RM_Receive.RawMaterial.MaterialNo,
                    ProductSKU = "",
                    LotNumber = i.GIV_RM_Receive.BatchNo ?? string.Empty,
                    SerialNumber = string.Empty,
                    Quantity = 1,
                    ExpirationDate = DateTime.Now,
                    ReceivedDate = i.GIV_RM_Receive.ReceivedDate,
                    Status = InventoryStatus.Reserved.ToString(),
                    MainHU = i.PalletCode ?? string.Empty
                }).ToList());
                details.Inventories.AddRange(location.GIVFGReceivePallets.Select(i => new InventoryItemViewModel
                {
                    Id = i.Id,
                    ProductName = i.Receive.FinishedGood.Description ?? string.Empty,
                    ProductSKU = i.Receive.FinishedGood.SKU ?? string.Empty,
                    LotNumber = i.Receive.BatchNo ?? string.Empty,
                    SerialNumber = string.Empty,
                    Quantity = 1,
                    ExpirationDate = DateTime.Now,
                    ReceivedDate = i.Receive.ReceivedDate,
                    Status = InventoryStatus.Reserved.ToString(),
                    MainHU = i.PalletCode ?? string.Empty
                }).ToList());

                details.CurrentItems = details.Inventories.Count();
                details.StatusName = location.IsEmpty || details.CurrentItems == 0
                             ? AppConsts.LocationGridStatus.AVAILABLE
                             : (details.CurrentItems < location.MaxItems
                                 ? AppConsts.LocationGridStatus.PARTIAL
                                 : AppConsts.LocationGridStatus.OCCUPIED);

                return Json(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location details for {LocationId}", locationId);
                return Json(new { error = "Failed to load location details" });
            }
        }

        #region Modal UI Link inventory
        [HttpGet]
        public async Task<IActionResult> GetAvailableLinkableItems([FromQuery] GetLinkableItemsRequestDto request)
        {
            try
            {
                _logger.LogDebug("Getting available linkable items for location {LocationId}", request.LocationId);

                var result = await _locationService.GetAvailableLinkableItemsAsync(request);

                return Json(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Location not found: {LocationId}", request.LocationId);
                return NotFound(new { error = "Location not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available linkable items for location {LocationId}", request.LocationId);
                return Json(new { error = "Failed to load available items" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkItemsToLocation([FromBody] LinkItemsToLocationRequestDto request)
        {
            try
            {
                _logger.LogInformation("Linking {ItemCount} items to location {LocationId}",
                    request.Items?.Count ?? 0, request.LocationId);

                if (request.Items == null || !request.Items.Any())
                {
                    return BadRequest(new { error = "No items selected for linking" });
                }

                var result = await _locationService.LinkItemsToLocationAsync(request);

                if (result.Success)
                {
                    var update = await _locationGridHelper.GetLocationUpdateDtoByLocationIdAsync(request.LocationId);

                    // Trigger SignalR update
                    await _warehouseNotificationService.SendLocationUpdateAsync(update);

                    foreach(var locationId in result.PreviousLocationIds)
                    {
                        var update2 = await _locationGridHelper.GetLocationUpdateDtoByLocationIdAsync(locationId);

                        // Trigger SignalR update
                        await _warehouseNotificationService.SendLocationUpdateAsync(update2);
                    }

                    return Json(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking items to location {LocationId}", request.LocationId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while linking items to location"
                });
            }
        }
        #endregion

        [HttpPost]
        public async Task<IActionResult> GenerateLayoutPDF(Guid zoneId, bool includeAllLocations = false, string? statusFilter = null, 
            string? rowFilter = null, string? searchTerm = null)
        {
            try
            {
                _logger.LogInformation("Generating layout PDF for zone {ZoneId}, includeAll: {IncludeAll}, filters: {StatusFilter}/{RowFilter}/{SearchTerm}",
                    zoneId, includeAllLocations, statusFilter, rowFilter, searchTerm);

                // Validate zone exists and user has access
                var zone = await _context.Zones
                    .Include(z => z.Warehouse)
                    .FirstOrDefaultAsync(z => z.Id == zoneId && !z.IsDeleted);

                if (zone == null)
                {
                    _logger.LogWarning("Zone {ZoneId} not found for PDF generation", zoneId);
                    return NotFound(new { message = "Zone not found" });
                }

                // Check user access to warehouse
                var currentWarehouseId = _currentUserService.CurrentWarehouseId;
                var isAdmin = _currentUserService.IsInRole(AppConsts.Roles.SYSTEM_ADMIN);

                if (!isAdmin && zone.WarehouseId != currentWarehouseId)
                {
                    _logger.LogWarning("User {UserId} denied access to zone {ZoneId} - wrong warehouse",
                        _currentUserService.UserId, zoneId);
                    throw new Exception("Access denied to this zone");
                }

                // Build query for locations
                var query = _context.Locations
                    .Where(l => l.ZoneId == zoneId && !l.IsDeleted)
                    .Include(l => l.Inventories.Where(i => !i.IsDeleted))
                    .Include(l => l.GIVRMReceivePallets.Where(i => !i.IsDeleted))
                    .Include(l => l.GIVFGReceivePallets.Where(i => !i.IsDeleted))
                    .AsQueryable();

                // Apply filters only if not including all locations
                if (!includeAllLocations)
                {
                    // Apply status filter
                    if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
                    {
                        if (statusFilter == "available")
                        {
                            query = query.Where(l => l.IsEmpty);
                        }
                        else if (statusFilter == "partial" || statusFilter == "occupied")
                        {
                            query = query.Where(l => !l.IsEmpty);
                        }
                    }

                    // Apply row filter
                    if (!string.IsNullOrEmpty(rowFilter) && rowFilter != "all")
                    {
                        query = query.Where(l => l.Row == rowFilter);
                    }

                    // Apply search filter
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        var searchLower = searchTerm.ToLower();
                        query = query.Where(l =>
                            l.Code.ToLower().Contains(searchLower) ||
                            l.Name.ToLower().Contains(searchLower) ||
                            (l.Barcode != null && l.Barcode.ToLower().Contains(searchLower)));
                    }
                }

                // Execute query and transform to grid items
                var locations = await query
                    .Select(l => new
                    {
                        Location = l,
                        TotalItemCount = l.Inventories.Count(i => !i.IsDeleted) +
                                       l.GIVRMReceivePallets.Count(i => !i.IsDeleted) +
                                       l.GIVFGReceivePallets.Count(i => !i.IsDeleted)
                    })
                    .Select(x => new LocationGridItemDto
                    {
                        Id = x.Location.Id,
                        Code = x.Location.Code,
                        Barcode = x.Location.Barcode ?? string.Empty,
                        Name = x.Location.Name,
                        Row = x.Location.Row ?? "",
                        Bay = x.Location.Bay ?? 0,
                        Level = x.Location.Level ?? 0,
                        IsEmpty = x.Location.IsEmpty,
                        Status = x.Location.IsEmpty || x.TotalItemCount == 0
                            ? LocationStatus.Available
                            : (x.TotalItemCount < x.Location.MaxItems
                                ? LocationStatus.Partial
                                : LocationStatus.Occupied),
                        StatusName = x.Location.IsEmpty || x.TotalItemCount == 0
                            ? AppConsts.LocationGridStatus.AVAILABLE
                            : (x.TotalItemCount < x.Location.MaxItems
                                ? AppConsts.LocationGridStatus.PARTIAL
                                : AppConsts.LocationGridStatus.OCCUPIED),
                        InventoryCount = x.TotalItemCount,
                        TotalQuantity = x.Location.Inventories.Where(i => !i.IsDeleted).Sum(i => i.Quantity),
                        MaxWeight = x.Location.MaxWeight,
                        MaxVolume = x.Location.MaxVolume,
                        MaxItems = x.Location.MaxItems
                    })
                    .ToListAsync();

                var filteredLocations = (string.IsNullOrEmpty(statusFilter) || statusFilter == "all")
                    ? locations
                    : locations.Where(x => x.StatusName.ToLower() == statusFilter.ToLower()).ToList();
                _logger.LogInformation("Found {LocationCount} locations for PDF generation in zone {ZoneId}",
                    filteredLocations.Count, zoneId);

                // Generate PDF using the PDF generator service
                var pdfBytes = _pdfHelper.GenerateLayoutPDF(new LocationGridPDFRequest
                {
                    Zone = zone,
                    Locations = filteredLocations,
                    IncludeAllLocations = includeAllLocations,
                    Filters = new LocationGridFilters
                    {
                        StatusFilter = statusFilter,
                        RowFilter = rowFilter,
                        SearchTerm = searchTerm
                    },
                    GeneratedAt = DateTime.Now
                });

                // Generate filename as specified
                var zoneName = zone.Name.Replace(" ", "_").Replace("/", "_"); // Clean zone name for filename
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"LocationGridLayout_{zoneName}_{timestamp}.pdf";

                _logger.LogInformation("Successfully generated layout PDF for zone {ZoneId}, file size: {FileSize} bytes, filename: {FileName}",
                    zoneId, pdfBytes.Length, fileName);

                // Return PDF file
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating layout PDF for zone {ZoneId}", zoneId);
                return StatusCode(500, new { message = "Failed to generate PDF layout. Please try again." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnlimitedLocationCurrentItems(Guid locationId, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            try
            {
                var location = await _context.Locations
                    .Include(l => l.Inventories.Where(i => !i.IsDeleted))
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.Client)
                    .Include(l => l.GIVFGReceivePallets.Where(i => !i.IsDeleted))
                        .ThenInclude(i => i.Receive)
                            .ThenInclude(r => r.FinishedGood)
                    .Include(l => l.GIVRMReceivePallets.Where(i => !i.IsDeleted))
                        .ThenInclude(i => i.GIV_RM_Receive)
                            .ThenInclude(r => r.RawMaterial)
                    .FirstOrDefaultAsync(l => l.Id == locationId && !l.IsDeleted);

                if (location == null)
                    return NotFound();

                // Combine all items into a single list
                var allItems = new List<object>();

                // Add inventory items
                allItems.AddRange(location.Inventories.Select(i => new
                {
                    Id = i.Id,
                    Type = "Inventory",
                    Name = i.Product.Name,
                    SKU = i.Product.SKU,
                    Client = i.Product.Client?.Name ?? "",
                    Quantity = i.Quantity,
                    ReceivedDate = i.ReceivedDate,
                    BatchLot = i.LotNumber ?? ""
                }));

                // Add GIV FG items
                allItems.AddRange(location.GIVFGReceivePallets.Select(i => new
                {
                    Id = i.Id,
                    Type = "GIV FG Pallet",
                    Name = i.Receive.FinishedGood.Description ?? "",
                    SKU = i.Receive.FinishedGood.SKU ?? "",
                    Client = "Givaudan",
                    Quantity = 1,
                    ReceivedDate = i.Receive.ReceivedDate,
                    BatchLot = i.Receive.BatchNo ?? "",
                    MainHU = i.PalletCode
                }));

                // Add GIV RM items
                allItems.AddRange(location.GIVRMReceivePallets.Select(i => new
                {
                    Id = i.Id,
                    Type = "GIV RM Pallet",
                    Name = i.GIV_RM_Receive.RawMaterial.MaterialNo,
                    SKU = "",
                    Client = "Givaudan",
                    Quantity = 1,
                    ReceivedDate = i.GIV_RM_Receive.ReceivedDate,
                    BatchLot = i.GIV_RM_Receive.BatchNo ?? "",
                    MainHU = i.PalletCode
                }));

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    allItems = allItems.Where(i =>
                        i.GetType().GetProperty("Name")?.GetValue(i)?.ToString()?.ToLower().Contains(searchLower) == true ||
                        i.GetType().GetProperty("MainHU")?.GetValue(i)?.ToString()?.ToLower().Contains(searchLower) == true ||
                        i.GetType().GetProperty("Client")?.GetValue(i)?.ToString()?.ToLower().Contains(searchLower) == true
                    ).ToList();
                }

                // Apply pagination
                var totalItems = allItems.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var pagedItems = allItems
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new
                {
                    items = pagedItems,
                    currentPage = page,
                    totalPages = totalPages,
                    totalItems = totalItems,
                    pageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unlimited location current items for {LocationId}", locationId);
                return Json(new { error = "Failed to load current items" });
            }
        }
    }
}