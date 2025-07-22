using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_FG_Receive")]
    public class GIV_FG_Receive:TenantEntity
    {
        public TransportType TypeID { get; set; }
        public virtual GIV_FinishedGood FinishedGood { get; set; } = default!;
        public Guid? FinishedGoodId { get; set; }
        public string? BatchNo { get; set; }
        public DateTime ReceivedDate{ get; set; }
        public string? ReceivedBy { get; set; }
        public string? Remarks { get; set; }
        public string? PO { get; set; }
        public Guid? PackageTypeId { get; set; }
        public virtual GeneralCode? PackageType { get; set; } = null!;
        public virtual ICollection<GIV_FG_ReceivePallet> FG_ReceivePallets { get; set; } = new List<GIV_FG_ReceivePallet>();
    }
}
