
namespace WMS.Domain.DTOs.Locations
{
    /// <summary>
    /// Request DTO for linking items to location
    /// </summary>
    public class LinkItemsToLocationRequestDto
    {
        public Guid LocationId { get; set; }
        public List<LinkableItemRequestDto> Items { get; set; } = new();
        public bool IgnoreCapacity { get; set; }
    }

}
