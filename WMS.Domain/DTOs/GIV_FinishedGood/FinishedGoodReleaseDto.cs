using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_RM_Receive;

namespace WMS.Domain.DTOs.GIV_FinishedGood
{
    public class FinishedGoodReleaseDto
    {
        public Guid FinishedGoodId { get; set; }
        public string SKU { get; set; } = default!;
        public string? Description { get; set; }
        public List<FG_ReceiveReleaseDto> Receives { get; set; } = new();
    }
}
