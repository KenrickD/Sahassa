namespace WMS.WebApp.Models.GeneralCodes
{
    public class GeneralCodeListViewModel
    {
        public List<GeneralCodeTypeItemViewModel> CodeTypes { get; set; } = new List<GeneralCodeTypeItemViewModel>();
        public int TotalCount { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }
}
