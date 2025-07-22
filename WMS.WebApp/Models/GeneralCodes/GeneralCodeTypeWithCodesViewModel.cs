namespace WMS.WebApp.Models.GeneralCodes
{
    public class GeneralCodeTypeWithCodesViewModel
    {
        public GeneralCodeTypeViewModel CodeType { get; set; } = new GeneralCodeTypeViewModel();
        public List<GeneralCodeViewModel> Codes { get; set; } = new List<GeneralCodeViewModel>();
        public bool HasEditAccess { get; set; }
    }
}
