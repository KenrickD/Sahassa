
namespace WMS.Domain.DTOs.GIV_Container
{
    public class ContainerPalletDto
    {
        public Guid Id { get; set; }
        public Guid ReceiveId { get; set; }
        public string? PalletCode { get; set; }
        public int PackSize { get; set; }
        public int Quantity { get; set; }
        public int QuantityBalance { get; set; }
        public int ItemCount { get; set; }
        public string? LocationName { get; set; }
        public bool Group3 { get; set; }
        public bool Group6 { get; set; }
        public bool Group8 { get; set; }
        public bool Group9 { get; set; }
        public bool NDG { get; set; }
        public bool Scentaurus { get; set; }
        public string MaterialNo { get; set; } = string.Empty;
        public string? BatchNo { get; set; }
    }
}
