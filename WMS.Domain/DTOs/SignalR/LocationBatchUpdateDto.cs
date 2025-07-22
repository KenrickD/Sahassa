namespace WMS.Domain.DTOs.SignalR
{
    /// <summary>
    /// Batch update container for throttling multiple updates
    /// </summary>
    public class LocationBatchUpdateDto
    {
        /// <summary>
        /// List of location updates
        /// </summary>
        public List<LocationUpdateDto> Updates { get; set; } = new List<LocationUpdateDto>();

        /// <summary>
        /// Batch timestamp
        /// </summary>
        public DateTime BatchTime { get; set; }

        /// <summary>
        /// Warehouse ID for this batch
        /// </summary>
        public Guid WarehouseId { get; set; }

        /// <summary>
        /// Total number of updates in this batch
        /// </summary>
        public int TotalUpdates => Updates.Count;
    }
}
