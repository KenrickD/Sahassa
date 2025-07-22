using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceiveItem;

namespace WMS.Domain.DTOs.GIV_RM_Receive
{
    public class RM_ReceiveCreateDto
    {
        public int TypeID { get; set; }
        [StringLength(100)]
        public string? BatchNo { get; set; } = default!;
        public Guid? ContainerId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string ReceivedBy { get; set; } = default!;
        public string? Remarks { get; set; }
        public string? PO { get; set; } = default!;
        public string? PackageType { get; set; }
        public Guid? GroupId { get; set; }
        public List<ReceivePalletCreateDto> RM_ReceivePallets { get; set; } = new();
    }
}
