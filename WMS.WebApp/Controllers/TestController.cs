//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using WMS.Application.Helpers;
//using WMS.Application.Interfaces;
//using WMS.Domain.DTOs.SignalR;
//using WMS.Domain.Interfaces;
//using WMS.Infrastructure.Data;

//namespace WMS.WebApp.Controllers
//{
//    [ApiController]
//    [Route("test")]
//    public class TestController : ControllerBase
//    {
//        private readonly AppDbContext _context;
//        private readonly ICurrentUserService _currentUserService;
//        private readonly IWarehouseNotificationService _warehouseNotificationService;
//        private readonly LocationGridHelper _locationGridHelper;

//        public TestController(AppDbContext context,
//            ICurrentUserService currentUserService,
//            IWarehouseNotificationService warehouseNotificationService,
//            LocationGridHelper locationGridHelper)
//        {
//            _context = context;
//            _currentUserService = currentUserService;
//            _warehouseNotificationService = warehouseNotificationService;
//            _locationGridHelper = locationGridHelper;
//        }

//        [HttpPost("simple/{id}")]
//        [IgnoreAntiforgeryToken]  // Add this line
//        [AllowAnonymous]
//        public IActionResult TestSimple(string id)
//        {
//            return Ok($"Received ID: {id}, Timestamp: {DateTime.UtcNow}");
//        }

//        [HttpGet("ping")]
//        [AllowAnonymous]
//        public IActionResult Ping()
//        {
//            return Ok("Test controller is working!");
//        }

//        [HttpPost]
//        [AllowAnonymous]
//        [IgnoreAntiforgeryToken]
//        [Route("zone-location-update/{zoneId}/{locationCode}")]
//        public async Task<IActionResult> TestZoneLocationUpdate(string zoneId, string locationCode)
//        {
//            try
//            {
//                // Find the specific location by zone ID and location code
//                //var location = await _context.Locations
//                //    .Include(l => l.Zone)
//                //    .Include(l => l.Inventories.Where(i => !i.IsDeleted))
//                //    .Where(l => l.ZoneId == Guid.Parse(zoneId) &&
//                //               l.Code == locationCode &&
//                //               !l.IsDeleted)
//                //    .FirstOrDefaultAsync();

//                //if (location == null)
//                //{
//                //    return NotFound(new
//                //    {
//                //        error = $"Location '{locationCode}' not found in zone '{zoneId}'",
//                //        zoneId = zoneId,
//                //        locationCode = locationCode
//                //    });
//                //}

//                //var update = new LocationUpdateDto
//                //{
//                //    LocationId = location.Id,
//                //    LocationCode = location.Code,
//                //    ZoneId = location.ZoneId,
//                //    WarehouseId = location.Zone.WarehouseId,
//                //    IsEmpty = !location.IsEmpty,
//                //    Status = location.IsEmpty ? "occupied" : "available",
//                //    InventoryCount = location.Inventories?.Count(i => !i.IsDeleted) ?? 0,
//                //    TotalQuantity = location.Inventories?.Where(i => !i.IsDeleted).Sum(i => i.Quantity) ?? 0,
//                //    UpdateType = LocationUpdateType.StatusChanged,
//                //    UpdatedAt = DateTime.UtcNow,
//                //    UpdatedBy = _currentUserService.GetCurrentUsername,
//                //    Row = location.Row ?? "",
//                //    Bay = location.Bay ?? 0,
//                //    Level = location.Level ?? 0
//                //};

//                var locationUpdateDto = await _locationGridHelper.GetLocationUpdateDtoByZoneIdAndLocationCodeAsync(Guid.Parse(zoneId), locationCode);
//                // Store original status for response
//                var originalStatus = locationUpdateDto.IsEmpty ? "empty" : "occupied";
//                var newStatus = !locationUpdateDto.IsEmpty ? "empty" : "occupied";

//                await _warehouseNotificationService.SendLocationUpdateAsync(locationUpdateDto);

//                return Ok(new
//                {
//                    success = true,
//                    message = $"Test update sent for location '{locationCode}' in zone '{zoneId}'",
//                    location = new
//                    {
//                        id = locationUpdateDto.LocationId,
//                        code = locationUpdateDto.LocationCode,
//                        zoneId = locationUpdateDto.ZoneId,
//                        warehouseId = locationUpdateDto.WarehouseId,
//                        originalStatus = originalStatus,
//                        newStatus = newStatus,
//                        inventoryCount = locationUpdateDto.InventoryCount,
//                        row = locationUpdateDto.Row,
//                        bay = locationUpdateDto.Bay,
//                        level = locationUpdateDto.Level
//                    },
//                    signalRGroupName = $"Zone_{locationUpdateDto.WarehouseId}_{zoneId}",
//                    timestamp = DateTime.UtcNow
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    error = "Failed to send test update",
//                    details = ex.Message,
//                    zoneId = zoneId,
//                    locationCode = locationCode
//                });
//            }
//        }
//    }
//}
