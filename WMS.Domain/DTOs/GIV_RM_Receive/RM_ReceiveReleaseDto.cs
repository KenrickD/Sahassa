using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet.PalletDto;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;

namespace WMS.Domain.DTOs.GIV_RM_Receive
{
    public class RM_ReceiveReleaseDto
    {
        public Guid Id { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string BatchNo { get; set; } = default!;
        public string ReceivedBy { get; set; } = default!;
        public List<RM_PalletReleaseDto> Pallets { get; set; } = new();
    }
}
