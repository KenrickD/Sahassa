using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePallet
{
    public class RM_PalletReleaseDto
    {
        public Guid Id { get; set; }
        public string? PalletCode { get; set; }
        public bool IsReleased { get; set; }
        public string HandledBy { get; set; } = default!;
        public List<RM_PalletItemReleaseDto> Items { get; set; } = new();
    }
}
