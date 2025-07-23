using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class FinishedGoodConflictResponse
    {
        public bool HasConflicts { get; set; }
        public List<FinishedGoodConflictItem> Conflicts { get; set; } = new List<FinishedGoodConflictItem>();
        public string? Message { get; set; }
    }

    public class FinishedGoodConflictItem
    {
        public Guid PalletId { get; set; }
        public string PalletCode { get; set; } = string.Empty;
        public Guid? ItemId { get; set; }
        public string? ItemCode { get; set; }
        public DateTime ExistingReleaseDate { get; set; }
        public Guid ExistingJobId { get; set; }
        public string ConflictType { get; set; } = string.Empty; // "Pallet" or "Item"
    }
}
