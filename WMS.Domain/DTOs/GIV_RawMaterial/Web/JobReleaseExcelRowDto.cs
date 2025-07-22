using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class JobReleaseExcelRowDto
    {
        public DateTime ReleaseDate { get; set; }
        public string MaterialNo { get; set; } = string.Empty;
        public string MHU { get; set; } = string.Empty; 
        public string HU { get; set; } = string.Empty; 
        public string Batch { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}
