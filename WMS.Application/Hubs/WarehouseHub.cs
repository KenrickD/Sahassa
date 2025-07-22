using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WMS.Application.Interfaces;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;

namespace WMS.Application.Hubs
{
    /// <summary>
    /// SignalR hub for real-time warehouse operations
    /// Handles location updates, inventory movements, and future dashboard notifications
    /// </summary>
    [Authorize]
    public class WarehouseHub : Hub
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<WarehouseHub> _logger;
        private readonly IZoneService _zoneService;

        public WarehouseHub(
            ICurrentUserService currentUserService,
            IZoneService zoneService,
            ILogger<WarehouseHub> logger)
        {
            _currentUserService = currentUserService;
            _zoneService = zoneService;
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// User must explicitly join zone groups after connection
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var warehouseId = _currentUserService.CurrentWarehouseId;
                var userId = _currentUserService.UserId;
                var username = _currentUserService.GetCurrentUsername;

                if (warehouseId == Guid.Empty)
                {
                    _logger.LogWarning("User {UserId} ({Username}) attempted to connect without valid warehouse context",
                        userId, username);
                    Context.Abort();
                    return;
                }

                _logger.LogInformation("User {UserId} ({Username}) connected to WarehouseHub. ConnectionId: {ConnectionId}",
                    userId, username, Context.ConnectionId);

                // Store connection metadata for debugging/monitoring
                await Clients.Caller.SendAsync("Connected", new
                {
                    warehouseId = warehouseId,
                    connectionId = Context.ConnectionId,
                    connectedAt = DateTime.UtcNow,
                    message = "Connected - ready to join zone groups"
                });

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SignalR connection for user {UserId}", _currentUserService.UserId);
                Context.Abort();
            }
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// Automatically removes from all groups
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var username = _currentUserService.GetCurrentUsername;

                _logger.LogInformation("User {UserId} ({Username}) disconnected from WarehouseHub. ConnectionId: {ConnectionId}",
                    userId, username, Context.ConnectionId);

                if (exception != null)
                {
                    _logger.LogWarning(exception, "User {UserId} disconnected with exception", userId);
                }

                // SignalR automatically removes from groups on disconnect, no manual cleanup needed

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SignalR disconnection for user {UserId}", _currentUserService.UserId);
            }
        }

        /// <summary>
        /// Join a specific zone group to receive real-time updates for that zone only
        /// This is the main method clients use to subscribe to zone updates
        /// </summary>
        /// <param name="zoneId">Zone ID to join</param>
        public async Task JoinZoneGroup(Guid zoneId)
        {
            try
            {
                var warehouseId = _currentUserService.CurrentWarehouseId;
                var zone = await _zoneService.GetZoneByIdAsync(zoneId);

                // Validate that the zone belongs to the user's warehouse for security
                // You should add zone validation here in production

                var zoneGroupName = GetZoneGroupName(warehouseId, zoneId);

                await Groups.AddToGroupAsync(Context.ConnectionId, zoneGroupName);

                _logger.LogInformation("User {UserId} joined zone group {GroupName}",
                    _currentUserService.UserId, zoneGroupName);

                await Clients.Caller.SendAsync("JoinedZoneGroup", new
                {
                    zoneId,
                    groupName = zoneGroupName,
                    zoneName = zone.Name,
                    message = $"Now receiving real-time updates for zone {zone.Name}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining zone group {ZoneId} for user {UserId}",
                    zoneId, _currentUserService.UserId);

                await Clients.Caller.SendAsync("Error", new
                {
                    message = "Failed to join zone group",
                    zoneId
                });
            }
        }

        /// <summary>
        /// Leave a specific zone group to stop receiving updates for that zone
        /// </summary>
        /// <param name="zoneId">Zone ID to leave</param>
        public async Task LeaveZoneGroup(Guid zoneId)
        {
            try
            {
                var warehouseId = _currentUserService.CurrentWarehouseId;
                var zoneGroupName = GetZoneGroupName(warehouseId, zoneId);
                var zone = await _zoneService.GetZoneByIdAsync(zoneId);

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, zoneGroupName);

                _logger.LogInformation("User {UserId} left zone group {GroupName}",
                    _currentUserService.UserId, zoneGroupName);

                await Clients.Caller.SendAsync("LeftZoneGroup", new
                {
                    zoneId,
                    groupName = zoneGroupName,
                    zoneName = zone.Name,
                    message = $"Stopped receiving updates for zone {zone.Name}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving zone group {ZoneId} for user {UserId}",
                    zoneId, _currentUserService.UserId);

                await Clients.Caller.SendAsync("Error", new
                {
                    message = "Failed to leave zone group",
                    zoneId
                });
            }
        }

        /// <summary>
        /// Switch to a different zone - leaves current zone and joins new one
        /// This is the most common operation when user changes zone selection
        /// </summary>
        /// <param name="fromZoneId">Previous zone ID (null if no previous zone)</param>
        /// <param name="toZoneId">New zone ID to switch to</param>
        public async Task SwitchZoneGroup(Guid? fromZoneId, Guid toZoneId)
        {
            try
            {
                var warehouseId = _currentUserService.CurrentWarehouseId;
                var toZone = await _zoneService.GetZoneByIdAsync(toZoneId);

                // Leave previous zone group if specified
                if (fromZoneId.HasValue)
                {
                    var fromZoneGroupName = GetZoneGroupName(warehouseId, fromZoneId.Value);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, fromZoneGroupName);

                    _logger.LogDebug("User {UserId} left zone group {GroupName}",
                        _currentUserService.UserId, fromZoneGroupName);
                }

                // Join new zone group
                var toZoneGroupName = GetZoneGroupName(warehouseId, toZoneId);
                await Groups.AddToGroupAsync(Context.ConnectionId, toZoneGroupName);

                _logger.LogInformation("User {UserId} switched to zone group {GroupName}",
                    _currentUserService.UserId, toZoneGroupName);

                await Clients.Caller.SendAsync("SwitchedZoneGroup", new
                {
                    fromZoneId,
                    toZoneId,
                    groupName = toZoneGroupName,
                    zoneName = toZone.Name,
                    message = $"Switched to zone {toZone.Name} - receiving real-time updates"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching from zone {FromZoneId} to zone {ToZoneId} for user {UserId}",
                    fromZoneId, toZoneId, _currentUserService.UserId);

                await Clients.Caller.SendAsync("Error", new
                {
                    message = "Failed to switch zone group",
                    fromZoneId,
                    toZoneId
                });
            }
        }

        /// <summary>
        /// Ping method for connection health checking
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }

        /// <summary>
        /// Get zone group name for SignalR groups - this is the primary grouping method
        /// </summary>
        /// <param name="warehouseId">Warehouse ID</param>
        /// <param name="zoneId">Zone ID</param>
        /// <returns>Group name string</returns>
        public static string GetZoneGroupName(Guid warehouseId, Guid zoneId)
        {
            return $"Zone_{warehouseId}_{zoneId}";
        }

        /// <summary>
        /// Get warehouse group name for SignalR groups (for warehouse-wide notifications only)
        /// </summary>
        /// <param name="warehouseId">Warehouse ID</param>
        /// <returns>Group name string</returns>
        public static string GetWarehouseGroupName(Guid warehouseId)
        {
            return $"Warehouse_{warehouseId}";
        }
    }
}
