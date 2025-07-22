using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_Receive
{
    public class FG_ReceiveSummaryDto
    {
        public Guid Id { get; set; }
        public Guid? FinishedGoodId { get; set; }
        public DateTime ReceivedDate { get; set; }

        public int PackSize { get; set; }

        public int TotalQuantity { get; set; }
        public int TotalPallet { get; set; }

        public int BalanceQuantity { get; set; }
        public int BalancePallet { get; set; }
        public bool HasEditAccess { get; set; }
    }
}
