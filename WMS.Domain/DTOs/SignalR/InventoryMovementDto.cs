namespace WMS.Domain.DTOs.SignalR
{
    /// <summary>
    /// Inventory movement notification (future feature)
    /// </summary>
    public class InventoryMovementDto
    {
        /// <summary>
        /// Movement ID
        /// </summary>
        public Guid MovementId { get; set; }

        /// <summary>
        /// Inventory item ID
        /// </summary>
        public Guid InventoryId { get; set; }

        /// <summary>
        /// Product information
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Product SKU
        /// </summary>
        public string ProductSKU { get; set; } = string.Empty;

        /// <summary>
        /// From location ID (null for receiving)
        /// </summary>
        public Guid? FromLocationId { get; set; }

        /// <summary>
        /// From location code
        /// </summary>
        public string? FromLocationCode { get; set; }

        /// <summary>
        /// To location ID (null for shipping)
        /// </summary>
        public Guid? ToLocationId { get; set; }

        /// <summary>
        /// To location code
        /// </summary>
        public string? ToLocationCode { get; set; }

        /// <summary>
        /// Quantity moved
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Movement type
        /// </summary>
        public string MovementType { get; set; } = string.Empty;

        /// <summary>
        /// Movement timestamp
        /// </summary>
        public DateTime MovedAt { get; set; }

        /// <summary>
        /// User who performed the movement
        /// </summary>
        public string MovedBy { get; set; } = string.Empty;

        /// <summary>
        /// Warehouse ID
        /// </summary>
        public Guid WarehouseId { get; set; }
    }
}
