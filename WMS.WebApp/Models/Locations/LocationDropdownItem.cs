namespace WMS.WebApp.Models.Locations
{
    public class LocationDropdownItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public Guid ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string DisplayText => !string.IsNullOrEmpty(Code) ? $"{Name} ({Code})" : Name;
    }
}
