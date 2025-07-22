namespace WMS.WebApp.Models.Warehouses
{
    public class WarehouseItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Code { get; set; } = default!;
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public bool IsActive { get; set; }
        public int ClientCount { get; set; }
        public int ZoneCount { get; set; }
        public int LocationCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = default!;

        public string DisplayLocation => BuildLocationString();
        public string StatusBadge => IsActive ? "Active" : "Inactive";
        public string StatusClass => IsActive ? "success" : "danger";

        private string BuildLocationString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(City)) parts.Add(City);
            if (!string.IsNullOrEmpty(State)) parts.Add(State);
            if (!string.IsNullOrEmpty(Country)) parts.Add(Country);
            return string.Join(", ", parts);
        }
    }
}
