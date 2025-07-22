using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood
{
    public class FinishedGoodItemDto
    {
        public string HU { get; set; } = default!;
        public string? BatchNo { get; set; }
        public string MHU { get; set; } = default!;
        public DateTime? ProdDate { get; set; }
        public bool DG { get; set; }
        public bool IsReleased { get; set; } = false;
        public string? Remarks { get; set; }
        public string? Location { get; set; }
    }
}
