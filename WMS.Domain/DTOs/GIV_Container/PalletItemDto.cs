
namespace WMS.Domain.DTOs.GIV_Container
{
    public class PalletItemDto
    {
        public Guid Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string? BatchNo { get; set; }
        public DateTime? ProdDate { get; set; }
        public bool DG { get; set; }
        public string? Remarks { get; set; }
        public bool IsReleased { get; set; }
    }
}
