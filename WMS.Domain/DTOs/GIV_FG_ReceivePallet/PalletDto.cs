namespace WMS.Domain.DTOs.GIV_FG_ReceivePallet.PalletDto
{
    public class PalletDto
    {
        public Guid Id { get; set; }
        public string? PalletCode { get; set; }
        public Guid ReceiveId { get; set; } 
        public DateTime ReceivedDate { get; set; } 
    }
}
