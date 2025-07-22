using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial
{
    public class RawMaterialPalletDto
    {
        public Guid Id { get; set; }
        public string BatchNo { get; set; } = default!;
        public string PalletCode { get; set; } = default!;
        public string ItemCode { get; set; } = default!;
        public string MaterialNo { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime ReceivedDate { get; set; }
        public int ItemCount { get; set; }
        public string AllItemCodes { get; set; } = default!;
        public Guid RawMaterialId { get; set; }
        public bool Group3 { get; set; } = false;
        public bool Group4_1 { get; set; } = false;
        public bool Group6 { get; set; } = false;
        public bool Group8 { get; set; } = false;
        public bool Group9 { get; set; } = false;
        public bool NDG { get; set; } = false;
        public bool Scentaurus { get; set; } = false;
        public bool HasEditAccess { get; set; }
    }
}
