namespace WMS.WebApp.Models.Warehouses
{
    public class WarehouseFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
    }
}
