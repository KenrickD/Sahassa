namespace WMS.WebApp.Models.Zones
{
    public class ZoneItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int LocationCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
