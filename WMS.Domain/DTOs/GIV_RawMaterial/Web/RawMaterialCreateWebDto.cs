using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_Receive.Web;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class RawMaterialCreateWebDto
    {
        public string MaterialNo { get; set; } = default!;
        public string? Description { get; set; }

        public List<RM_ReceiveCreateWebDto> Receives { get; set; } = new();
    }
}
