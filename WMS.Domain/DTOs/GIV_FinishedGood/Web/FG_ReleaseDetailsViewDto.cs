using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class FG_ReleaseDetailsViewDto
    {
        public Guid Id { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string ReleasedBy { get; set; } = string.Empty;
        public string? ReleasedByFullName { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ActualReleaseDate { get; set; }
        public string? ActualReleasedBy { get; set; }

        // Summary information
        public int TotalPallets { get; set; }
        public int TotalItems { get; set; }
        public int EntirePalletCount { get; set; }
        public int IndividualItemCount { get; set; }
        public int ReleasedPallets { get; set; }
        public int ReleasedItems { get; set; }

        // Status
        public bool IsCompleted { get; set; }
        public string StatusText { get; set; } = string.Empty;

        // FinishedGood Info
        public Guid FinishedGoodId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Display helpers
        public string ReleasedByDisplay => !string.IsNullOrEmpty(ReleasedByFullName) ? $"{ReleasedBy} ({ReleasedByFullName})" : ReleasedBy;
        public double ProgressPercentage => (TotalPallets + TotalItems) > 0 ? Math.Round(((double)(ReleasedPallets + ReleasedItems) / (TotalPallets + TotalItems)) * 100, 1) : 0;
    }
}
