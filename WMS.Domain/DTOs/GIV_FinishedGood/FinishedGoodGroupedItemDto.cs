using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood
{
    public class FinishedGoodGroupedItemDto
    {
        public string? BatchNo { get; set; }
        public List<string> HUList { get; set; } = new();
        public List<string> MHUList { get; set; } = new();
        public int Qty { get; set; }
        public int BalQty { get; set; }
        public DateTime? ProdDate { get; set; }
        public List<bool> DG { get; set; } = new();
        public List<string?> Remarks { get; set; } = new();
        public List<string?> Location { get; set; } = new();
        public bool HasEditAccess { get; set; }
    }
}
