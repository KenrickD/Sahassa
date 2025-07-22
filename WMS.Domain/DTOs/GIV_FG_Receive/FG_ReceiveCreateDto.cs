using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;

namespace WMS.Domain.DTOs.GIV_FG_Receive
{
    public class FG_ReceiveCreateDto
    {
        public string? BatchNo { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? ReceivedBy { get; set; }
        public string? PO { get; set; }
        public string? PackageType { get; set; }
        public List<FG_ReceivePalletCreateDto> Pallets { get; set; } = new();
    }
}
