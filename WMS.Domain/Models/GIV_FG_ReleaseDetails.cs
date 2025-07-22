using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_FG_ReleaseDetails")]
    public class GIV_FG_ReleaseDetails:TenantEntity
    {
        public virtual GIV_FG_Release GIV_FG_Release { get; set; } = default!;
        public Guid GIV_FG_ReleaseId { get; set; }

        public virtual GIV_FG_Receive? GIV_FG_Receive { get; set; } = default!;
        public Guid? GIV_FG_ReceiveId { get; set; }

        public virtual GIV_FG_ReceivePallet GIV_FG_ReceivePallet { get; set; } = default!;
        public Guid GIV_FG_ReceivePalletId { get; set; }

        public virtual GIV_FG_ReceivePalletItem? GIV_FG_ReceivePalletItem { get; set; }
        public Guid? GIV_FG_ReceivePalletItemId { get; set; }
        public bool IsEntirePallet { get; set; } = true;
        public DateTime? ActualReleaseDate { get; set; }
        public string? ActualReleasedBy { get; set; }
    }
}
