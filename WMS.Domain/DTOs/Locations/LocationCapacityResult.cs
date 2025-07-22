namespace WMS.Domain.DTOs.Locations
{
    public class LocationCapacityResult
    {
        public bool CanAccommodate { get; set; }
        public int AvailableCapacity { get; set; }
        public int CurrentOccupancy { get; set; }
        public int MaxCapacity { get; set; }
    }
}
