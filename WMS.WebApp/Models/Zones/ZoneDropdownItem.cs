namespace WMS.WebApp.Models.Zones
{
    public class ZoneDropdownItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string DisplayText => !string.IsNullOrEmpty(Code) ? $"{Name} ({Code})" : Name;

    }
}
