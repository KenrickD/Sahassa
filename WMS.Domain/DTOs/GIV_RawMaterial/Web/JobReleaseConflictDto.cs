using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class JobReleaseConflictDto
    {
        public string MaterialNo { get; set; } = string.Empty;
        public Guid ReceiveId { get; set; }
        public string? PalletCode { get; set; }
        public string ConflictType { get; set; } = string.Empty;
        public Guid? ExistingJobId { get; set; }
        public List<string> ConflictingItems { get; set; } = new();
    }
}
