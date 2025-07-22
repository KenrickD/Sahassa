using WMS.WebApp.Models.Clients;

namespace WMS.WebApp.Models.Zones
{
    public class ZonePageViewModel
    {
        public ZoneViewModel Zone { get; set; } = new ZoneViewModel();
        public bool HasEditAccess { get; set; }
        public bool IsEdit { get; set; }
        public List<WarehouseDropdownItem> Warehouses { get; set; } = new List<WarehouseDropdownItem>();
    }
}
