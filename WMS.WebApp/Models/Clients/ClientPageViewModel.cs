namespace WMS.WebApp.Models.Clients
{
    public class ClientPageViewModel
    {
        public ClientViewModel Client { get; set; } = new ClientViewModel();
        public bool HasEditAccess { get; set; }
        public bool IsEdit { get; set; }
        public List<WarehouseDropdownItem> Warehouses { get; set; } = new List<WarehouseDropdownItem>();
    }
}
