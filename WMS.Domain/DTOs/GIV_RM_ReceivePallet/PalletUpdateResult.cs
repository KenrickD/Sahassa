namespace WMS.Domain.DTOs.GIV_RM_ReceivePallet
{
    public class PalletUpdateResult
    {
        public string Code { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string PalletType { get; set; } = string.Empty;
        public string? PreviousLocation { get; set; }
        public string? NewLocation { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
