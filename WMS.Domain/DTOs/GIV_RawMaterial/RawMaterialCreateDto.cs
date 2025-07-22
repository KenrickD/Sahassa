using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_RM_Receive;

namespace WMS.Domain.DTOs.RawMaterial
{
    public class RawMaterialCreateDto
    {
        [Required]
        [StringLength(100)]
        public string MaterialNo { get; set; } = default!;
        public string? Description { get; set; } = default!;
        public bool? Group3 { get; set; } = false;
        public bool? Group4_1 { get; set; } = false;
        public bool? Group6 { get; set; } = false;
        public bool? Group8 { get; set; } = false;
        public bool? Group9 { get; set; } = false;
        public bool? NDG { get; set; } = false;
        public bool? Scentaurus { get; set; } = false;
        public List<RM_ReceiveCreateDto> Receives { get; set; } = new();

    }
}
