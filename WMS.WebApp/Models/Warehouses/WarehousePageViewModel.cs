namespace WMS.WebApp.Models.Warehouses
{
    public class WarehousePageViewModel
    {
        public WarehouseFormViewModel Warehouse { get; set; } = new();
        public bool IsEdit { get; set; }
        public bool HasEditAccess { get; set; }
        public bool HasDeleteAccess { get; set; }
        public bool HasConfigAccess { get; set; }
    }
}
