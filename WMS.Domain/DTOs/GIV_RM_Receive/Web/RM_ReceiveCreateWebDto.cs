using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet.Web;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.GIV_RM_Receive.Web
{
    public class RM_ReceiveCreateWebDto
    {
        public TransportType TypeID { get; set; }
        public Guid? ContainerId { get; set; }
        public Guid? PackageTypeId { get; set; }
        public string? BatchNo { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string ReceivedBy { get; set; } = default!;
        public string? Remarks { get; set; }
        public string? PO { get; set; }

        public List<RM_ReceivePalletCreateWebDto> Pallets { get; set; } = new();
    }
}
