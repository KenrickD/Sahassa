
namespace WMS.Domain.DTOs.GIV_Container
{
    public class ContainerPalletsResponse
    {
        public List<ContainerPalletDto> Pallets { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string ContainerNo { get; set; } = string.Empty;
    }
}
