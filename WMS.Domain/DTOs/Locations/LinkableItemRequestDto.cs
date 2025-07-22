
namespace WMS.Domain.DTOs.Locations
{
    /// <summary>
    /// Individual item to link
    /// </summary>
    public class LinkableItemRequestDto
    {
        public Guid ItemId { get; set; }
        public LinkableItemType ItemType { get; set; }
    }
}
