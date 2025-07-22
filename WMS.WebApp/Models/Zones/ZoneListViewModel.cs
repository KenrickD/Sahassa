namespace WMS.WebApp.Models.Zones
{
    public class ZoneListViewModel
    {
        public List<ZoneItemViewModel> Zones { get; set; } = new List<ZoneItemViewModel>();
        public int TotalCount { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }
}
