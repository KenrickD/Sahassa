using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem.Web;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto.Web;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePallet.Web
{
    public class RM_ReceivePalletCreateWebDto
    {
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PalletCode must be exactly 11 digits")]
        public string? PalletCode { get; set; }
        public string HandledBy { get; set; } = default!;
        public Guid? LocationId { get; set; }
        public string? StoredBy { get; set; }
        [Required(ErrorMessage = "Pack Size is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Pack Size must be greater than 0")]
        public int PackSize { get; set; }

        public List<RM_ReceivePalletItemCreateWebDto> Items { get; set; } = new();
        public List<RM_ReceivePalletPhotoCreateWebDto> Photos { get; set; } = new();
    }
}
