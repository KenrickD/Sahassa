using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_Receive
{
    public class ReceiveViewDto
    {
        public DateTime ReceivedDate { get; set; }
        public int PackSize { get; set; }
        public int Qty { get; set; }
        public int Plt { get; set; }
        public int BalQty { get; set; }
        public int BalPlt { get; set; }
    }
}
