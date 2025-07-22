using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePalletItem
{
    public class RM_ReceivePalletItemAuditDto
    {
        public Guid Id { get; set; }
        public string ItemCode { get; set; } = default!;
        public string? BatchNo { get; set; }
        public DateTime ProdDate { get; set; }
        public bool DG { get; set; }
        public string? Remarks { get; set; }
    }
}
