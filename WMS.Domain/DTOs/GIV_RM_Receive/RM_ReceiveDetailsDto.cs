using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.DTOs.GIV_RawMaterial;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.GIV_RM_Receive
{
    public class RM_ReceiveDetailsDto
    {
        public Guid Id { get; set; }
        public TransportType TypeID { get; set; }
        [MaxLength(100)]
        public string BatchNo { get; set; } = default!;
        public DateTime ReceivedDate { get; set; }
        public string ReceivedBy { get; set; } = default!;
        public string? Remarks { get; set; }
        public Containers? Containers { get; set; }
        public RawMaterialDetailsDto? RawMaterial { get; set; }
        public List<RM_ReceivePalletDetailsDto>? RM_ReceivePallets { get; set; }
    }
}
