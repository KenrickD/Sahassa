using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePalletItem.Web
{
    public class RM_ReceivePalletItemCreateWebDto
    {
        public string ItemCode { get; set; } = default!;
        public string? BatchNo { get; set; }
        public DateTime? ProdDate { get; set; }
        public bool Group3 { get; set; } = false;
        public bool Group6 { get; set; } = false;
        public bool Group8 { get; set; } = false;
        public bool Group9 { get; set; } = false;
        public bool NDG { get; set; } = false;
        public bool Scentaurus { get; set; } = false;
        public string? Remarks { get; set; }
    }
}
