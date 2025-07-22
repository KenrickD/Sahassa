using WMS.WebApp.Models.Clients;
using WMS.WebApp.Models.Zones;

namespace WMS.WebApp.Models.Locations
{
    public class LocationBarcodeViewModel
    {
        public Guid? WarehouseId { get; set; }
        public List<WarehouseDropdownItem> Warehouses { get; set; } = new List<WarehouseDropdownItem>();

        public Guid? ZoneId { get; set; }
        public List<ZoneDropdownItem> Zones { get; set; } = new List<ZoneDropdownItem>();
    }
}
