using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_FG_Release")]
    public class GIV_FG_Release:TenantEntity
    {
        public virtual GIV_FinishedGood GIV_FinishedGood { get; set; } = default!;
        public Guid GIV_FinishedGoodId { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string ReleasedBy { get; set; } = default!;
        public DateTime? ActualReleaseDate { get; set; }
        public string? ActualReleasedBy { get; set; }
        public string? Remarks { get; set; }
        public virtual ICollection<GIV_FG_ReleaseDetails> GIV_FG_ReleaseDetails { get; set; } = new List<GIV_FG_ReleaseDetails>();
        public Guid? JobId { get; set; } = null;

    }
}
