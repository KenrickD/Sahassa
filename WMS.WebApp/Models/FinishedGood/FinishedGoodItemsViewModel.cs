using WMS.Domain.DTOs.GIV_FinishedGood;
using WMS.Domain.DTOs.GIV_RawMaterial;

namespace WMS.WebApp.Models.FinishedGood
{
    public class FinishedGoodItemsViewModel
    {
        public Guid ReceiveId { get; set; } = default!;
        public bool ShowGrouped { get; set; }
        public List<FinishedGoodItemDto> Items { get; set; } = new();
        public List<FinishedGoodGroupedItemDto> GroupedItems { get; set; } = new();
    }
}
