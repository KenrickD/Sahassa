namespace WMS.WebApp.Models.GeneralCodes
{
    public class GeneralCodeTypeItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int CodesCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<GeneralCodeItemViewModel> Codes { get; set; } = new List<GeneralCodeItemViewModel>();
        public bool IsExpanded { get; set; }
    }
}
