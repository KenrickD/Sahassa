using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_Receive;

namespace WMS.Domain.DTOs.GIV_RawMaterial
{
    public class RawMaterialDetailsDto
    {
        public Guid Id { get; set; }
        [StringLength(100)]
        public string MaterialNo { get; set; } = default!;
        public string Description { get; set; } = default!;
        public List<RM_ReceiveDetailsDto>? RM_Receive { get; set; }
        public bool Group3 { get; set; } = false;
        public bool Group4_1 { get; set; } = false;
        public bool Group6 { get; set; } = false;
        public bool Group8 { get; set; } = false;
        public bool Group9 { get; set; } = false;
        public bool NDG { get; set; } = false;
        public bool Scentaurus { get; set; } = false;

        public int TotalBalanceQty { get; set; }  
        public int TotalBalancePallet { get; set; }

        public bool HasEditAccess { get; set; }
    }
}
