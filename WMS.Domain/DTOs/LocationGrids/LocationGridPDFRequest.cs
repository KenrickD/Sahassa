using WMS.Domain.Models;

namespace WMS.Domain.DTOs.LocationGrids
{
    public class LocationGridPDFRequest
    {
        public Zone Zone { get; set; }
        public List<LocationGridItemDto> Locations { get; set; }
        public bool IncludeAllLocations { get; set; }
        public LocationGridFilters Filters { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
