using WMS.WebApp.Models.Zones;

namespace WMS.WebApp.Models.LocationGrids
{
    public class LocationGridViewModel
    {
        public List<ZoneDropdownItem> Zones { get; set; } = new List<ZoneDropdownItem>();
        public Guid SelectedZoneId { get; set; }
        public Guid WarehouseId { get; set; }
    }
}
