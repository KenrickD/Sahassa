using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_Receive;

namespace WMS.Domain.DTOs.GIV_FinishedGood
{
    public class FinishedGoodCreateDto
    {
        public string? SKU { get; set; }
        public string? Description { get; set; }
        public List<FG_ReceiveCreateDto> Receives { get; set; } = new();
    }
}
