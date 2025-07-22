using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class RawMaterialExportDto
    {
        public string MaterialNo { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ReceivedDate { get; set; }
        public int TotalPallets { get; set; }
        public string PalletCodes { get; set; } = string.Empty;
    }
}
