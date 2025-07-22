using WMS.WebApp.Models.Clients;
using WMS.WebApp.Models.GeneralCodes;

namespace WMS.WebApp.Models.Products
{
    public class ProductPageViewModel
    {
        public ProductViewModel Product { get; set; } = new ProductViewModel();
        public bool HasEditAccess { get; set; }
        public bool IsEdit { get; set; }
        public List<ClientDropdownItem> Clients { get; set; } = new List<ClientDropdownItem>();
        public List<GeneralCodeDropDownItem> ProductTypes { get; set; } = new List<GeneralCodeDropDownItem>();
        public List<GeneralCodeDropDownItem> ProductCategories { get; set; } = new List<GeneralCodeDropDownItem>();
        public List<GeneralCodeDropDownItem> ProductUnitOfMeasures { get; set; } = new List<GeneralCodeDropDownItem>();
    }
}
