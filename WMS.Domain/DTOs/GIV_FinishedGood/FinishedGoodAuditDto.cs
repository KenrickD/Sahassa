using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood
{
    public class FinishedGoodAuditDto
    {
        public Guid Id { get; set; }
        public string? SKU { get; set; }
        public string? Description { get; set; }
    }
}
