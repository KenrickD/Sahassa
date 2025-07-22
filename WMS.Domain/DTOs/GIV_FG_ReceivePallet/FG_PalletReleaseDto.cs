using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePallet
{
    public class FG_PalletReleaseDto
    {
        public Guid Id { get; set; }
        public string? PalletCode { get; set; }
        public bool IsReleased { get; set; }
        public string HandledBy { get; set; } = default!;
        public List<FG_PalletItemReleaseDto> Items { get; set; } = new();
    }
}
