using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_Receive
{
    public class FG_ReceiveAuditDto
    {
        public Guid Id { get; set; }
        public string? BatchNo { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? ReceivedBy { get; set; }
    }

}
