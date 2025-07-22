using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_RM_Receive;

namespace WMS.Domain.DTOs.GIV_FinishedGood
{
    public class FinishedGoodDetailsDto
    {

        public Guid Id { get; set; }
        public string SKU { get; set; } = default!;
        public string Description { get; set; } = default!;
        public List<FG_ReceiveDetailsDto>? FG_Receive { get; set; }

        public int TotalBalanceQty { get; set; }
        public int TotalBalancePallet { get; set; }
        public int TotalQty { get; set; }
        public int TotalPallet { get; set; }

        public bool HasEditAccess { get; set; }

        // Group fields
        public bool Group3 { get; set; }
        public bool Group4_1 { get; set; }
        public bool Group6 { get; set; }
        public bool Group8 { get; set; }
        public bool Group9 { get; set; }
        public bool NDG { get; set; }
        public bool Scentaurus { get; set; }
    }
}
