
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.Locations
{
    /// <summary>
    /// Request DTO for getting available linkable items
    /// </summary>
    public class GetLinkableItemsRequestDto
    {
        public Guid LocationId { get; set; }
        public string? SearchTerm { get; set; }
        public Guid? ClientId { get; set; }
        public LinkableItemType? ItemType { get; set; }
        public bool IgnoreCapacity { get; set; }
        public string? ZoneName { get; set; }
        public Location? Location { get; set; }
    }
}
