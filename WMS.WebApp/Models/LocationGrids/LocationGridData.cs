using WMS.WebApp.Models.Zones;

namespace WMS.WebApp.Models.LocationGrids
{
    public class LocationGridData
    {
        public List<LocationGridItem> Locations { get; set; } = new List<LocationGridItem>();
        public ZoneItemViewModel? Zone { get; set; } 
        public int MaxRow { get; set; }
        public int MaxBay { get; set; }
        public int MaxLevel { get; set; }
    }
}
