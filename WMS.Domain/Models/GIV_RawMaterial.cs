using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_RawMaterial")]
    public class GIV_RawMaterial: TenantEntity
    {
        [MaxLength(100)]
        public string MaterialNo { get; set; } = default!;
        public string? Description { get; set; }
        public bool Group3 { get; set; } = false;
        public bool Group4_1 { get; set; } = false;
        public bool Group6 { get; set; } = false;
        public bool Group8 { get; set; } = false;
        public bool Group9 { get; set; } = false;
        public bool NDG { get; set; } = false;
        public bool Scentaurus { get; set; } = false;
        public virtual ICollection<GIV_RM_Receive> RM_Receive { get; set; } = new List<GIV_RM_Receive>();
        public string? PackSize { get; set; }
    }
}
