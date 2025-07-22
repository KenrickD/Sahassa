
namespace WMS.Domain.DTOs.Locations
{
    /// <summary>
    /// Response DTO for linking operation
    /// </summary>
    public class LinkItemsToLocationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int LinkedItemsCount { get; set; }
        public int NewLocationItemCount { get; set; }
        public string NewLocationStatus { get; set; } = string.Empty;
        public List<Guid> PreviousLocationIds { get; set; } = new List<Guid>();
    }
}
