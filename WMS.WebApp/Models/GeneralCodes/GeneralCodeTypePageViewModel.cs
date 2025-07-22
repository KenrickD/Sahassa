using WMS.WebApp.Models.Clients;

namespace WMS.WebApp.Models.GeneralCodes
{
    public class GeneralCodeTypePageViewModel
    {
        public GeneralCodeTypeViewModel CodeType { get; set; } = new GeneralCodeTypeViewModel();
        public bool HasEditAccess { get; set; }
        public bool IsEdit { get; set; }
        public List<WarehouseDropdownItem> Warehouses { get; set; } = new List<WarehouseDropdownItem>();
    }
}
