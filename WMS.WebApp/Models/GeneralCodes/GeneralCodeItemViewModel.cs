namespace WMS.WebApp.Models.GeneralCodes
{
    public class GeneralCodeItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public int Sequence { get; set; }
        public Guid GeneralCodeTypeId { get; set; }
    }
}
