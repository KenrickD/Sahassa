using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_RM_Release")]
    public class GIV_RM_Release : TenantEntity
    {
        public virtual GIV_RawMaterial GIV_RawMaterial { get; set; } = default!;
        public Guid GIV_RawMaterialId { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string ReleasedBy { get; set; } = default!;
        public DateTime? ActualReleaseDate { get; set; }
        public string? ActualReleasedBy { get; set; }
        public string? Remarks { get; set; }
        public virtual ICollection< GIV_RM_ReleaseDetails> GIV_RM_ReleaseDetails { get; set; } = new List<GIV_RM_ReleaseDetails>();
        public Guid? JobId { get; set; } = null;

    }
}
