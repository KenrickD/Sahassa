namespace WMS.Domain.DTOs.GIV_RM_ReceivePallet
{
    public class BulkUpdatePalletLocationResultDto
    {
        public int TotalRequested { get; set; }
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationBarcode { get; set; } = string.Empty;
        public List<PalletUpdateResult> SuccessfulUpdates { get; set; } = new();
        public List<PalletUpdateResult> FailedUpdates { get; set; } = new();
    }
}
