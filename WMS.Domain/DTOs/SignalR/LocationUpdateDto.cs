namespace WMS.Domain.DTOs.SignalR
{
    /// <summary>
    /// Real-time location update data transfer object
    /// </summary>
    public class LocationUpdateDto
    {
        /// <summary>
        /// Location ID
        /// </summary>
        public Guid LocationId { get; set; }

        /// <summary>
        /// Location code (e.g., "A01-01")
        /// </summary>
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// Zone ID this location belongs to
        /// </summary>
        public Guid ZoneId { get; set; }

        /// <summary>
        /// Warehouse ID this location belongs to
        /// </summary>
        public Guid WarehouseId { get; set; }

        /// <summary>
        /// Whether the location is empty
        /// </summary>
        public bool IsEmpty { get; set; }

        /// <summary>
        /// Location status (available, occupied, reserved, maintenance, blocked)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Number of inventory items in this location
        /// </summary>
        public int InventoryCount { get; set; }

        /// <summary>
        /// Total quantity of all items in this location
        /// </summary>
        public decimal TotalQuantity { get; set; }

        /// <summary>
        /// Current weight utilization
        /// </summary>
        public decimal CurrentWeight { get; set; }

        /// <summary>
        /// Current volume utilization
        /// </summary>
        public decimal CurrentVolume { get; set; }

        /// <summary>
        /// Update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// User who triggered the update
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Update type for frontend handling
        /// </summary>
        public LocationUpdateType UpdateType { get; set; }

        /// <summary>
        /// Row letter for grid positioning
        /// </summary>
        public string Row { get; set; } = string.Empty;

        /// <summary>
        /// Bay number for grid positioning
        /// </summary>
        public int Bay { get; set; }

        /// <summary>
        /// Level number for grid positioning
        /// </summary>
        public int Level { get; set; }
    }
    /// <summary>
    /// Type of location update for frontend handling
    /// </summary>
    public enum LocationUpdateType
    {
        /// <summary>
        /// Status changed (empty to occupied, etc.)
        /// </summary>
        StatusChanged,

        /// <summary>
        /// Inventory added to location
        /// </summary>
        InventoryAdded,

        /// <summary>
        /// Inventory removed from location
        /// </summary>
        InventoryRemoved,

        /// <summary>
        /// Inventory moved from this location
        /// </summary>
        InventoryMoved,

        /// <summary>
        /// Location utilization updated
        /// </summary>
        UtilizationChanged,

        /// <summary>
        /// Location blocked/unblocked
        /// </summary>
        AccessChanged
    }
}
