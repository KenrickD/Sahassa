
using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePallet
{
    public class BulkUpdatePalletLocationDto
    {
        [Required]
        public List<string>? PalletCodes { get; set; }
        [Required]
        public string LocationBarcode { get; set; } = string.Empty;
        [Required]
        public string StoredBy { get; set; } = string.Empty;
    }
}
