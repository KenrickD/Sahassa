using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePallet
{
    public class RM_ReceivePalletAuditDto
    {
        public Guid Id { get; set; }
        public string PalletCode { get; set; } = default!;
        public string HandledBy { get; set; } = default!;
        public int PackSize { get; set; }
    }
}
