using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePallet
{
    public class FG_ReceivePalletCreateDto
    {
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PalletCode must be exactly 11 digits.")]
        public string? PalletCode { get; set; }
        public string? HandledBy { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string? ReceivedBy { get; set; }
        public int? PackSize { get; set; }
        public List<FG_ReceivePalletItemCreateDto?> Items { get; set; } = new();
        //public List<FG_ReceivePalletPhotoCreateDto?> Photos { get; set; } = new();
    }
}
