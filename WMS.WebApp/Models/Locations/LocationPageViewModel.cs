using WMS.WebApp.Models.Clients;
using WMS.WebApp.Models.Zones;

namespace WMS.WebApp.Models.Locations
{
    public class LocationPageViewModel
    {
        public LocationViewModel Location { get; set; } = new LocationViewModel();
        public bool HasEditAccess { get; set; }
        public bool IsEdit { get; set; }
        public List<WarehouseDropdownItem> Warehouses { get; set; } = new List<WarehouseDropdownItem>();
        public List<ZoneDropdownItem> Zones { get; set; } = new List<ZoneDropdownItem>();
        public List<string> TemperatureZoneOptions { get; set; } = new List<string>
        {
            "Ambient", "Chilled", "Frozen", "Heated", "Controlled"
        };
    }
}
