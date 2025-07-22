namespace WMS.Domain.DTOs.SignalR
{
    /// <summary>
    /// Dashboard notification (future feature)
    /// </summary>
    public class DashboardNotificationDto
    {
        /// <summary>
        /// Notification ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Notification title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Notification message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Notification type (info, warning, error, success)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Notification icon
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Auto-dismiss after seconds (null = manual dismiss)
        /// </summary>
        public int? AutoDismissAfter { get; set; }

        /// <summary>
        /// Notification timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Warehouse ID this notification belongs to
        /// </summary>
        public Guid WarehouseId { get; set; }

        /// <summary>
        /// Target user roles (null = all users)
        /// </summary>
        public List<string>? TargetRoles { get; set; }
    }
}
