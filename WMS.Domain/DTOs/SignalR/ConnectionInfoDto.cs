namespace WMS.Domain.DTOs.SignalR
{
    /// <summary>
    /// Connection info for debugging/monitoring
    /// </summary>
    public class ConnectionInfoDto
    {
        /// <summary>
        /// Connection ID
        /// </summary>
        public string ConnectionId { get; set; } = string.Empty;

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Warehouse ID
        /// </summary>
        public Guid WarehouseId { get; set; }

        /// <summary>
        /// Connected timestamp
        /// </summary>
        public DateTime ConnectedAt { get; set; }

        /// <summary>
        /// User's IP address
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent
        /// </summary>
        public string? UserAgent { get; set; }
    }
}
