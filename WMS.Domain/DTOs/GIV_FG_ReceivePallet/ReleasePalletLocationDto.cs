using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePallet
{
    public class ReleasePalletLocationDto
    {
        public string Code { get; set; } = default!;
        public string? LocationId { get; set; }
    }
}
