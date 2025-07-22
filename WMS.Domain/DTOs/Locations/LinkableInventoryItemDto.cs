
namespace WMS.Domain.DTOs.Locations
{
    /// <summary>
    /// DTO for items that can be linked to a location (Inventory, GIV FG Pallets, GIV RM Pallets)
    /// </summary>
    public class LinkableInventoryItemDto
    {
        /// <summary>
        /// Unique identifier for the item
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Type of the linkable item
        /// </summary>
        public LinkableItemType Type { get; set; }

        /// <summary>
        /// Display name for the item
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// SKU or Code for the item
        /// </summary>
        public string SKUCode { get; set; } = string.Empty;

        /// <summary>
        /// Client name who owns this item
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// Date when item was received
        /// </summary>
        public DateTime ReceiveDate { get; set; }

        /// <summary>
        /// Additional details for display (lot number, batch number, etc.)
        /// </summary>
        public string? AdditionalInfo { get; set; }
        
        /// <summary>
        /// Current item location & Zone
        /// </summary>
        public string? LocationAndZoneName { get; set; }
    }

    /// <summary>
    /// Types of items that can be linked to locations
    /// </summary>
    public enum LinkableItemType
    {
        Inventory = 1,
        GIV_FG_Pallet = 2,
        GIV_RM_Pallet = 3
    }
}
