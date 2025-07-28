using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class JobReleaseIndividualReleaseDto
    {
        public Guid ReleaseId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string FinishedGoodDescription { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public int PalletCount { get; set; }
        public int ItemCount { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasDeleteAccess { get; set; }
        // Display helpers
        public string ReleaseTypeText
        {
            get
            {
                var parts = new List<string>();
                if (PalletCount > 0)
                    parts.Add($"{PalletCount} Pallet(s)");
                if (ItemCount > 0)
                    parts.Add($"{ItemCount} Item(s)");
                return string.Join(", ", parts);
            }
        }
    }
}
