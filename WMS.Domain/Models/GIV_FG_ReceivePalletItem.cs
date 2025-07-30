using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_FG_ReceivePalletItem")]
    public class GIV_FG_ReceivePalletItem : TenantEntity
    {
        public Guid GIV_FG_ReceivePalletId { get; set; }
        public virtual GIV_FG_ReceivePallet GIV_FG_ReceivePallet { get; set; } = default!;
        public Guid? FinishedGoodId { get; set; }
        public virtual GIV_FinishedGood FinishedGood { get; set; } = default!;

        [MaxLength(100)]
        public string ItemCode { get; set; } = default!;
        public string? BatchNo { get; set; }
        public DateTime? ProdDate{ get; set; }
        public bool IsReleased { get; set; } = false;
        public string? Remarks { get; set; }
    }
}
