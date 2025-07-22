using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_ReceiveItemPhoto;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;

namespace WMS.Domain.DTOs.GIV_RM_ReceiveItem
{
    public class ReceivePalletCreateDto
    {
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PalletCode must be exactly 11 digits.")]
        public string PalletCode { get; set; } = default!;
        public string HandledBy { get; set; } = default!;
        public int PackSize { get; set; }
        public List<ReceivePalletItemCreateDto> RM_ReceivePalletItems { get; set; } = new();
        //public List<RMReceivePalletPhoto> RM_ReceivePalletPhotos { get; set; } = new();
    }
}
