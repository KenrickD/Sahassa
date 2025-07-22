namespace WMS.WebApp.Models.Clients
{
    public class WarehouseDropdownItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayText => !string.IsNullOrEmpty(Code) ? $"{Name} ({Code})" : Name;
    }
}