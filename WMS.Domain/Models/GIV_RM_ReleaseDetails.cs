using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_RM_ReleaseDetails")]
    public class GIV_RM_ReleaseDetails :TenantEntity
    {
        public virtual GIV_RM_Release GIV_RM_Release { get; set; } = default!;  
        public Guid GIV_RM_ReleaseId { get; set; }
        public virtual GIV_RM_Receive GIV_RM_Receive { get; set; } = default!;
        public Guid GIV_RM_ReceiveId { get; set; }
        public virtual GIV_RM_ReceivePalletItem? GIV_RM_ReceivePalletItem { get; set; } = default!;
        public Guid? GIV_RM_ReceivePalletItemId { get; set; }

        public virtual GIV_RM_ReceivePallet? GIV_RM_ReceivePallet { get; set; } 
        public Guid? GIV_RM_ReceivePalletId { get; set; } 
        public bool IsEntirePallet { get; set; } = false;
        public DateTime? ActualReleaseDate { get; set; }
        public string? ActualReleasedBy { get; set; }
    }
}
