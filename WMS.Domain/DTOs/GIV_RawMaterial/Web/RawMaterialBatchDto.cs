using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class RawMaterialBatchDto
    {
        public Guid Id { get; set; }
        public string BatchNo { get; set; } = "N/A";
        public string PalletCode { get; set; } = "N/A";
        public string ItemCode { get; set; } = default!;
        public string MaterialNo { get; set; } = default!;
        public string Description { get; set; } = "N/A";
        public bool Group3 { get; set; } = false;
        public bool Group4_1 { get; set; } = false;
        public bool Group6 { get; set; } = false;
        public bool Group8 { get; set; } = false;
        public bool Group9 { get; set; } = false;
        public bool NDG { get; set; } = false;
        public bool Scentaurus { get; set; } = false;
        public DateTime ReceivedDate { get; set; }
        public Guid RawMaterialId { get; set; }
        public bool HasEditAccess { get; set; }
    }
}
