using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Application.Attributes;
using WMS.Domain.DTOs.SignalR;
using WMS.Application.Helpers;

namespace WMS.WebApp.Controllers
{
    [ApiController]
    [Route("internal-api/notifications")]
    [IgnoreAntiforgeryToken] // Add this attribute
    public class NotificationsController : ControllerBase
    {
        private readonly IWarehouseNotificationService _warehouseNotificationService;
        private readonly ILogger<NotificationsController> _logger;
        private readonly LocationGridHelper _locationGridHelper;

        public NotificationsController(
            IWarehouseNotificationService warehouseNotificationService,
            LocationGridHelper locationGridHelper,
            ILogger<NotificationsController> logger)
        {
            _warehouseNotificationService = warehouseNotificationService;
            _locationGridHelper = locationGridHelper;
            _logger = logger;
        }

        [HttpPost("location-update")]
        [ServiceToken("location-update")]
        public async Task<IActionResult> LocationUpdate([FromBody] LocationUpdateAPIRequestDto requestDto)
        {
            try
            {
                // Get service info from JWT claims
                var serviceName = User.FindFirst("service")?.Value ?? "Unknown";
                var tokenId = User.FindFirst("jti")?.Value;

                _logger.LogInformation("Received location update from service: {ServiceName}, Token: {TokenId}, Location: {LocationCode}",
                    serviceName, tokenId, requestDto.LocationCodes.First());

                var update = await _locationGridHelper.GetLocationUpdateDtoByZoneIdAndLocationCodeAsync(requestDto.ZoneId, requestDto.LocationCodes.First());

                await _warehouseNotificationService.SendLocationUpdateAsync(update);

                _logger.LogInformation("SignalR notification sent successfully for location: {LocationCode}", update.LocationCode);

                return Ok(new
                {
                    success = true,
                    message = "Notification sent successfully",
                    calledBy = serviceName,
                    locationCode = update.LocationCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process location update notification");
                return StatusCode(500, new { error = "Failed to process notification" });
            }
        }

        [HttpPost("bulk-location-update")]
        [ServiceToken("location-update")]
        public async Task<IActionResult> BulkLocationUpdate([FromBody] LocationUpdateAPIRequestDto requestDto)
        {
            try
            {
                // Get service info from JWT claims
                var serviceName = User.FindFirst("service")?.Value ?? "Unknown";
                var tokenId = User.FindFirst("jti")?.Value;

                _logger.LogInformation("Received location update from service: {ServiceName}, Token: {TokenId}, Location: {LocationCode}",
                    serviceName, tokenId, string.Join(",", requestDto.LocationCodes ?? Enumerable.Empty<string>()));

                var updates = await _locationGridHelper.GetLocationUpdateDtosByZoneIdAndLocationCodesAsync(requestDto.ZoneId, requestDto.LocationCodes!);

                await _warehouseNotificationService.SendLocationUpdatesAsync(updates);

                _logger.LogInformation("SignalR notification sent successfully for location: {LocationCode}", string.Join(",", requestDto.LocationCodes ?? Enumerable.Empty<string>()));

                return Ok(new
                {
                    success = true,
                    message = "Notification sent successfully",
                    calledBy = serviceName,
                    locationCodes = requestDto.LocationCodes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process location update notification");
                return StatusCode(500, new { error = "Failed to process notification" });
            }
        }

        [HttpGet("health")]
        [ServiceToken]
        public IActionResult Health()
        {
            var serviceName = User.FindFirst("service")?.Value ?? "Unknown";
            return Ok(new
            {
                status = "healthy",
                service = serviceName,
                timestamp = DateTime.UtcNow
            });
        }
    }
}