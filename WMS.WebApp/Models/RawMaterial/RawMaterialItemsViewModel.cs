using WMS.Domain.DTOs.GIV_RawMaterial;

namespace WMS.WebApp.Models.RawMaterial
{
    public class RawMaterialItemsViewModel
    {
        public Guid ReceiveId { get; set; }
        public bool ShowGrouped { get; set; }
        public List<RawMaterialItemDto> Items { get; set; } = new();
        public List<RawMaterialGroupedItemDto> GroupedItems { get; set; } = new();
        public bool IsGroupedReceive { get; set; }
        public Guid? GroupId { get; set; }
    }

}
