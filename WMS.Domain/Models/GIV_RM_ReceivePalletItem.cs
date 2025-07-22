using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_RM_ReceivePalletItem")]
    public class GIV_RM_ReceivePalletItem : TenantEntity
    {
        public Guid GIV_RM_ReceivePalletId { get; set; }
        public virtual GIV_RM_ReceivePallet GIV_RM_ReceivePallet { get; set; } = default!;
        [MaxLength(100)]
        public string ItemCode { get; set; } = default!;
        public string? BatchNo { get; set; }
        public DateTime? ProdDate { get; set; }
        public bool IsReleased { get; set; } = false;
        public string? Remarks { get; set; }
    }
}
