using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class FinishedGoodForJobReleaseDto
    {
        public Guid Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int BalanceQty { get; set; }
        public int BalancePallets { get; set; }
        public int TotalReceives { get; set; }
    }
}
