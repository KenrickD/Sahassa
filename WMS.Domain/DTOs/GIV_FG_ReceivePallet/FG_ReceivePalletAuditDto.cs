using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePallet
{
    public class FG_ReceivePalletAuditDto
    {
        public Guid Id { get; set; }
        public string? PalletCode { get; set; }
        public string? HandledBy { get; set; }
    }

}
