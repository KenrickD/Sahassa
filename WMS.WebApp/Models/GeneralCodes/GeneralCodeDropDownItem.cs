
namespace WMS.WebApp.Models.GeneralCodes
{
    public class GeneralCodeDropDownItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public int Sequence {  get; set; }
        public string DisplayText => !string.IsNullOrEmpty(Detail) ? $"{Name} ({Detail})" : Name;
    }
}
