namespace WMS.WebApp.Models.Products
{
    public class ProductListViewModel
    {
        public List<ProductItemViewModel> Products { get; set; } = new List<ProductItemViewModel>();
        public int TotalCount { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }
}
