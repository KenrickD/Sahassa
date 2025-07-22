namespace WMS.WebApp.Models.Locations
{
    public class LocationListViewModel
    {
        public List<LocationItemViewModel> Locations { get; set; } = new List<LocationItemViewModel>();
        public int TotalCount { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }
}
