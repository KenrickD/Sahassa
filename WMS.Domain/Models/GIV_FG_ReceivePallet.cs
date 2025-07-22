using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_FG_ReceivePallet")]
    public class GIV_FG_ReceivePallet:TenantEntity
    {
        public Guid? ReceiveId { get; set; }
        public virtual GIV_FG_Receive Receive { get; set; } = null!;
        public Guid? LocationId { get; set; }
        public virtual Location Location { get; set; } = null!;
        [MaxLength (11)]
        public string? PalletCode { get; set; }
        public string? HandledBy { get; set; }
        public string? StoredBy { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? ReceivedBy { get; set; } 
        public int PackSize { get; set; }
        public bool IsReleased { get; set; } = false;
        public virtual ICollection<GIV_FG_ReceivePalletItem> FG_ReceivePalletItems { get; set; } = new List<GIV_FG_ReceivePalletItem>();
        public virtual ICollection<GIV_FG_ReceivePalletPhoto> FG_ReceivePalletPhotos { get; set; } = new List<GIV_FG_ReceivePalletPhoto>();
    }
}
