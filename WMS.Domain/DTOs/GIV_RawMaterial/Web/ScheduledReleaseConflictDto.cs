using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class ScheduledReleaseConflictDto
    {
        public Guid MaterialId { get; set; }
        public Guid? JobId { get; set; }
        public bool IsEntirePallet { get; set; }
        public Guid? PalletId { get; set; }
        public Guid ItemId { get; set; }
        public string? PalletCode { get; set; }
        public string? ItemCode { get; set; }
    }
}
