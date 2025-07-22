using WMS.Domain.DTOs.SignalR;

namespace WMS.Application.Interfaces
{
    /// <summary>
    /// Service interface for sending real-time warehouse notifications
    /// </summary>
    public interface IWarehouseNotificationService
    {
        /// <summary>
        /// Send location update to users viewing the specific zone (zone-based targeting)
        /// </summary>
        /// <param name="locationUpdate">Location update data</param>
        Task SendLocationUpdateAsync(LocationUpdateDto locationUpdate);

        /// <summary>
        /// Send location updates in batch to users viewing the affected zones
        /// </summary>
        /// <param name="locationUpdates">List of location updates</param>
        Task SendLocationUpdatesAsync(IEnumerable<LocationUpdateDto> locationUpdates);

        /// <summary>
        /// Send warehouse-wide notification to all users in the warehouse (for system alerts)
        /// </summary>
        /// <param name="warehouseId">Warehouse ID</param>
        /// <param name="notification">Dashboard notification</param>
        Task SendWarehouseNotificationAsync(Guid warehouseId, DashboardNotificationDto notification);

        /// <summary>
        /// Send inventory movement notification to users viewing affected zones
        /// </summary>
        /// <param name="movementUpdate">Inventory movement data</param>
        Task SendInventoryMovementAsync(InventoryMovementDto movementUpdate);

        /// <summary>
        /// Send notification to specific users by roles
        /// </summary>
        /// <param name="warehouseId">Warehouse ID</param>
        /// <param name="message">Message to send</param>
        /// <param name="targetRoles">Target user roles</param>
        Task SendToRolesAsync(Guid warehouseId, object message, params string[] targetRoles);

        /// <summary>
        /// Get active connection count for warehouse
        /// </summary>
        /// <param name="warehouseId">Warehouse ID</param>
        Task<int> GetActiveConnectionsAsync(Guid warehouseId);
    }
}
