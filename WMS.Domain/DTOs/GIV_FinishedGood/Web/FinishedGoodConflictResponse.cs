using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RawMaterial.Web;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class FinishedGoodConflictResponse
    {
        // Change from List<FinishedGoodConflictItem> to Dictionary structure like raw material
        public Dictionary<Guid, ConflictInfo> Pallets { get; set; } = new Dictionary<Guid, ConflictInfo>();
        public Dictionary<Guid, ConflictInfo> Items { get; set; } = new Dictionary<Guid, ConflictInfo>();

        // Keep these for backward compatibility if needed
        public bool HasConflicts { get; set; }
        public string Message { get; set; } = string.Empty;
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
