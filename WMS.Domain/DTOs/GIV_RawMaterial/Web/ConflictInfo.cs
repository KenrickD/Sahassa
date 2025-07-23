using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class ConflictInfo
    {
        public string Type { get; set; } = string.Empty;
        public Guid? JobId { get; set; }
        public string? PalletCode { get; set; }
        public string? ItemCode { get; set; }
    }
}
