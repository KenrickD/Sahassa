using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet.PalletDto;

namespace WMS.Domain.DTOs.GIV_FG_Receive
{
    public class FG_ReceiveGroupDto
    {
        public Guid ReceiveId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public List<PalletDto> Pallets { get; set; } = new();
    }
}
