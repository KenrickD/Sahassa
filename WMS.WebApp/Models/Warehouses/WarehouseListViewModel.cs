using WMS.WebApp.Models.Users;

namespace WMS.WebApp.Models.Warehouses
{
    public class WarehouseListViewModel
    {
        public List<WarehouseItemViewModel> Warehouses { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public WarehouseFilterViewModel Filter { get; set; } = new();
        public bool HasCreateAccess { get; set; }
        public bool HasEditAccess { get; set; }
        public bool HasDeleteAccess { get; set; }
    }
}
