using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_Receive;

namespace WMS.Domain.DTOs.GIV_RawMaterial
{
    public class RawMaterialReleaseDto
    {
        public Guid RawMaterialId { get; set; }
        public string MaterialNo { get; set; } = default!;
        public string? Description { get; set; }
        public List<RM_ReceiveReleaseDto> Receives { get; set; } = new();
    }
}
