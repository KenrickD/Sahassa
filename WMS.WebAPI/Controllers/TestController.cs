//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using WMS.Application.Helpers;
//using WMS.Application.Interfaces;
//using WMS.Application.Services;
//using WMS.Domain.DTOs.Locations;
//using WMS.Domain.DTOs.SignalR;
//using WMS.Domain.Interfaces;
//using WMS.Infrastructure.Data;
//using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

//namespace WMS.WebAPI.Controllers
//{
//    [ApiController]
//    [Route("test")]
//    public class TestController : ControllerBase
//    {
//        private readonly AppDbContext _context;
//        private readonly ICurrentUserService _currentUserService;
//        private readonly IAPIService _apiService;
//        private readonly ILocationService _locationService;
//        private readonly LocationGridHelper _locationGridHelper;

//        public TestController(AppDbContext context,
//            ICurrentUserService currentUserService,
//            IAPIService apiService,
//            ILocationService locationService,
//            LocationGridHelper locationGridHelper)
//        {
//            _context = context;
//            _currentUserService = currentUserService;
//            _apiService = apiService;
//            _locationService = locationService;
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
//        //[AllowAnonymous]
//        [Authorize]
//        [IgnoreAntiforgeryToken]
//        [Route("zone-location-update/{zoneId}/{locationCode}")]
//        public async Task<IActionResult> TestZoneLocationUpdate(string zoneId, string locationCode)
//        {
//            try
//            {
//                var locationUpdateDto = await _locationGridHelper.GetLocationUpdateDtoByZoneIdAndLocationCodeAsync(Guid.Parse(zoneId), locationCode);
//                // Store original status for response
//                var originalStatus = locationUpdateDto.IsEmpty ? "empty" : "occupied";
//                var newStatus = !locationUpdateDto.IsEmpty ? "empty" : "occupied";

//                if (locationUpdateDto.Status == "available")
//                {
//                    await _locationService.MarkLocationAsOccupiedAsync(locationUpdateDto.LocationId);
//                }
//                else
//                {
//                    await _locationService.MarkLocationAsEmptyAsync(locationUpdateDto.LocationId);
//                }
//                //await _apiService.NotifyLocationUpdateAsync(Guid.Parse(zoneId), locationCode);

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
//        [HttpPost]
//        [AllowAnonymous]
//        [IgnoreAntiforgeryToken]
//        [Route("zone-bulk-location-update")]
//        public async Task<IActionResult> TestZoneBulkLocationUpdate(LocationUpdateAPIRequestDto requestDto)
//        {
//            try
//            {
//                var locationUpdateDtos = await _locationGridHelper.GetLocationUpdateDtosByZoneIdAndLocationCodesAsync(requestDto.ZoneId, requestDto.LocationCodes);
//                foreach(var dto in locationUpdateDtos)
//                {
//                    if (dto.Status == "available")
//                    {
//                        await _locationService.MarkLocationAsOccupiedAsync(dto.LocationId);
//                    }
//                    else
//                    {
//                        await _locationService.MarkLocationAsEmptyAsync(dto.LocationId);
//                    }
//                }
//                //await _apiService.NotifyBulkLocationUpdateAsync(requestDto.ZoneId, requestDto.LocationCodes);
//                return Ok(new
//                {
//                    success = true,
//                    message = $"Test update sent for locations '{string.Join(",", requestDto.LocationCodes ?? Enumerable.Empty<string>())}' in zone '{requestDto.ZoneId}'",
//                    locations = locationUpdateDtos.Select(dto => new
//                    {
//                        id = dto.LocationId,
//                        code = dto.LocationCode,
//                        zoneId = dto.ZoneId,
//                        warehouseId = dto.WarehouseId,
//                        originalStatus = dto.IsEmpty ? "empty" : "occupied",
//                        newStatus = !dto.IsEmpty ? "empty" : "occupied",
//                        inventoryCount = dto.InventoryCount,
//                        row = dto.Row,
//                        bay = dto.Bay,
//                        level = dto.Level
//                    }),
//                    timestamp = DateTime.UtcNow
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    error = "Failed to send test update",
//                    details = ex.Message,
//                    zoneId = requestDto.ZoneId,
//                    locationCodes = requestDto.LocationCodes
//                });
//            }
//        }
//    }
//}
