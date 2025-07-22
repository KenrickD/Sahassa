using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_RM_ReceivePallet")]
    public class GIV_RM_ReceivePallet : TenantEntity
    {
        public Guid GIV_RM_ReceiveId { get; set; }
        public virtual GIV_RM_Receive GIV_RM_Receive { get; set; } = default!;
        [MaxLength(11)]
        public string? PalletCode { get; set; }
        public string HandledBy { get; set; } = default!;
        public Guid? LocationId { get; set; }
        public virtual Location Location { get; set; } = default!;
        public string? StoredBy { get; set; }
        public int PackSize { get; set; }
        public bool IsReleased { get; set; } = false;
        public virtual ICollection<GIV_RM_ReceivePalletItem> RM_ReceivePalletItems { get; set; } = new List<GIV_RM_ReceivePalletItem>();
        public virtual ICollection<GIV_RM_ReceivePalletPhoto> RM_ReceivePalletPhotos { get; set; } = new List<GIV_RM_ReceivePalletPhoto>();
    }
}
