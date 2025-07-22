using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using WMS.Application.Hubs;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs.SignalR;

namespace WMS.Application.Services
{
    /// <summary>
    /// Service implementation for sending real-time warehouse notifications with throttling
    /// </summary>
    public class WarehouseNotificationService : IWarehouseNotificationService
    {
        private readonly IHubContext<WarehouseHub> _hubContext;
        private readonly ILogger<WarehouseNotificationService> _logger;

        // Throttling: Store pending updates per location to batch them
        private readonly ConcurrentDictionary<Guid, LocationUpdateDto> _pendingLocationUpdates = new();
        private readonly ConcurrentDictionary<Guid, Timer> _locationUpdateTimers = new();
        private readonly object _timerLock = new object();

        // Throttling interval: 1 second as requested
        private readonly TimeSpan _throttleInterval = TimeSpan.FromSeconds(1);

        public WarehouseNotificationService(
            IHubContext<WarehouseHub> hubContext,
            ILogger<WarehouseNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Send location update with throttling to zone-specific groups
        /// Only users viewing this zone will receive the update
        /// </summary>
        public async Task SendLocationUpdateAsync(LocationUpdateDto locationUpdate)
        {
            try
            {
                if (locationUpdate == null)
                {
                    _logger.LogWarning("Attempted to send null location update");
                    return;
                }

                // Update the pending update for this location (overwrites previous if exists)
                _pendingLocationUpdates.AddOrUpdate(locationUpdate.LocationId, locationUpdate, (key, existing) => locationUpdate);

                // Set or reset the timer for this location
                lock (_timerLock)
                {
                    // Dispose existing timer if it exists
                    if (_locationUpdateTimers.TryGetValue(locationUpdate.LocationId, out var existingTimer))
                    {
                        existingTimer.Dispose();
                    }

                    // Create new timer that will fire after throttle interval
                    var timer = new Timer(async _ => await ProcessLocationUpdate(locationUpdate.LocationId),
                        null, _throttleInterval, Timeout.InfiniteTimeSpan);

                    _locationUpdateTimers[locationUpdate.LocationId] = timer;
                }

                _logger.LogDebug("Location update queued for throttling: {LocationId} ({LocationCode}) in zone {ZoneId}",
                    locationUpdate.LocationId, locationUpdate.LocationCode, locationUpdate.ZoneId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing location update for {LocationId}", locationUpdate.LocationId);
            }
        }

        /// <summary>
        /// Process throttled location update - sends to zone-specific group only
        /// </summary>
        private async Task ProcessLocationUpdate(Guid locationId)
        {
            try
            {
                // Get the latest pending update for this location
                if (!_pendingLocationUpdates.TryRemove(locationId, out var locationUpdate))
                {
                    _logger.LogDebug("No pending update found for location {LocationId}", locationId);
                    return;
                }

                // Clean up the timer
                lock (_timerLock)
                {
                    if (_locationUpdateTimers.TryRemove(locationId, out var timer))
                    {
                        timer.Dispose();
                    }
                }

                // Send the update to the ZONE group only (not entire warehouse)
                var zoneGroupName = WarehouseHub.GetZoneGroupName(locationUpdate.WarehouseId, locationUpdate.ZoneId);

                await _hubContext.Clients.Group(zoneGroupName)
                    .SendAsync("LocationUpdated", locationUpdate);

                _logger.LogDebug("Location update sent to zone group {GroupName}: {LocationId} ({LocationCode}) - Status: {Status}",
                    zoneGroupName, locationUpdate.LocationId, locationUpdate.LocationCode, locationUpdate.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing location update for {LocationId}", locationId);
            }
        }

        /// <summary>
        /// Send multiple location updates as a batch to zone-specific groups
        /// </summary>
        public async Task SendLocationUpdatesAsync(IEnumerable<LocationUpdateDto> locationUpdates)
        {
            try
            {
                if (locationUpdates == null || !locationUpdates.Any())
                {
                    _logger.LogDebug("No location updates to send");
                    return;
                }

                // Group updates by zone (not warehouse) for more granular targeting
                var updatesByZone = locationUpdates.GroupBy(u => new { u.WarehouseId, u.ZoneId });

                foreach (var zoneGroup in updatesByZone)
                {
                    var warehouseId = zoneGroup.Key.WarehouseId;
                    var zoneId = zoneGroup.Key.ZoneId;
                    var updates = zoneGroup.ToList();

                    var batchUpdate = new LocationBatchUpdateDto
                    {
                        Updates = updates,
                        BatchTime = DateTime.UtcNow,
                        WarehouseId = warehouseId
                    };

                    // Send to zone-specific group
                    var zoneGroupName = WarehouseHub.GetZoneGroupName(warehouseId, zoneId);

                    await _hubContext.Clients.Group(zoneGroupName)
                        .SendAsync("LocationBatchUpdated", batchUpdate);

                    _logger.LogInformation("Batch location update sent to zone {ZoneId} in warehouse {WarehouseId}: {UpdateCount} updates",
                        zoneId, warehouseId, updates.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending batch location updates");
            }
        }

        /// <summary>
        /// Send warehouse-wide notification (for system alerts, not location updates)
        /// </summary>
        public async Task SendWarehouseNotificationAsync(Guid warehouseId, DashboardNotificationDto notification)
        {
            try
            {
                if (notification == null)
                {
                    _logger.LogWarning("Attempted to send null warehouse notification");
                    return;
                }

                // Use warehouse group for system-wide notifications
                var warehouseGroupName = WarehouseHub.GetWarehouseGroupName(warehouseId);

                await _hubContext.Clients.Group(warehouseGroupName)
                    .SendAsync("WarehouseNotification", notification);

                _logger.LogDebug("Warehouse notification sent to warehouse {WarehouseId}: {Title}",
                    warehouseId, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending warehouse notification");
            }
        }

        /// <summary>
        /// Send inventory movement notification to users viewing affected zones
        /// </summary>
        public async Task SendInventoryMovementAsync(InventoryMovementDto movementUpdate)
        {
            try
            {
                if (movementUpdate == null)
                {
                    _logger.LogWarning("Attempted to send null inventory movement");
                    return;
                }

                // Send to zones involved in the movement
                var zonesToNotify = new HashSet<Guid>();

                // Add from zone if exists
                if (movementUpdate.FromLocationId.HasValue)
                {
                    // You'll need to add ZoneId to InventoryMovementDto or look it up
                    // For now, we'll send to warehouse group
                }

                // Add to zone if exists  
                if (movementUpdate.ToLocationId.HasValue)
                {
                    // You'll need to add ZoneId to InventoryMovementDto or look it up
                    // For now, we'll send to warehouse group
                }

                // For now, send to warehouse group until we add zone info to movement
                var warehouseGroupName = WarehouseHub.GetWarehouseGroupName(movementUpdate.WarehouseId);

                await _hubContext.Clients.Group(warehouseGroupName)
                    .SendAsync("InventoryMoved", movementUpdate);

                _logger.LogDebug("Inventory movement sent to warehouse {WarehouseId}: {ProductSKU} from {FromLocation} to {ToLocation}",
                    movementUpdate.WarehouseId, movementUpdate.ProductSKU,
                    movementUpdate.FromLocationCode, movementUpdate.ToLocationCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending inventory movement notification");
            }
        }

        /// <summary>
        /// Send dashboard notification (future feature)
        /// </summary>
        public async Task SendDashboardNotificationAsync(DashboardNotificationDto notification)
        {
            try
            {
                if (notification == null)
                {
                    _logger.LogWarning("Attempted to send null dashboard notification");
                    return;
                }

                var warehouseGroupName = WarehouseHub.GetWarehouseGroupName(notification.WarehouseId);

                await _hubContext.Clients.Group(warehouseGroupName)
                    .SendAsync("DashboardNotification", notification);

                _logger.LogDebug("Dashboard notification sent to warehouse {WarehouseId}: {Title}",
                    notification.WarehouseId, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending dashboard notification");
            }
        }

        /// <summary>
        /// Send notification to specific roles within a warehouse
        /// </summary>
        public async Task SendToRolesAsync(Guid warehouseId, object message, params string[] targetRoles)
        {
            try
            {
                if (targetRoles == null || !targetRoles.Any())
                {
                    _logger.LogWarning("No target roles specified for notification");
                    return;
                }

                var warehouseGroupName = WarehouseHub.GetWarehouseGroupName(warehouseId);

                await _hubContext.Clients.Group(warehouseGroupName)
                    .SendAsync("RoleBasedNotification", new
                    {
                        message,
                        targetRoles,
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogDebug("Role-based notification sent to warehouse {WarehouseId} for roles: {Roles}",
                    warehouseId, string.Join(", ", targetRoles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending role-based notification");
            }
        }

        /// <summary>
        /// Get active connection count for warehouse (future feature for monitoring)
        /// </summary>
        public async Task<int> GetActiveConnectionsAsync(Guid warehouseId)
        {
            try
            {
                // Note: This is a basic implementation
                // For production, you might want to store connection info in a distributed cache
                var warehouseGroupName = WarehouseHub.GetWarehouseGroupName(warehouseId);

                // This is an approximation - SignalR doesn't provide direct group member count
                // You would need to implement connection tracking for exact counts
                _logger.LogDebug("Active connections requested for warehouse {WarehouseId}", warehouseId);

                return await Task.FromResult(0); // Placeholder implementation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active connections for warehouse {WarehouseId}", warehouseId);
                return 0;
            }
        }

        /// <summary>
        /// Cleanup method for disposing timers (call this in DI container disposal)
        /// </summary>
        public void Dispose()
        {
            try
            {
                lock (_timerLock)
                {
                    foreach (var timer in _locationUpdateTimers.Values)
                    {
                        timer.Dispose();
                    }
                    _locationUpdateTimers.Clear();
                }
                _pendingLocationUpdates.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing WarehouseNotificationService");
            }
        }
    }
}
