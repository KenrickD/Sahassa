
namespace WMS.Domain.DTOs.GeneralCodes
{
    public class GeneralCodeWithTypeDto
    {
        public GeneralCodeTypeDto CodeType { get; set; } = new();
        public List<GeneralCodeDto> Codes { get; set; } = new();
        public bool IsExpanded { get; set; }
    }
}
