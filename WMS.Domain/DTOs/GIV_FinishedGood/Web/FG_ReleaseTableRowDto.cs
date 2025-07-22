using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class FG_ReleaseTableRowDto
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

        // Status information
        public bool IsCompleted { get; set; }
        public bool IsPartiallyReleased { get; set; }
        public int ReleasedPallets { get; set; }
        public int ReleasedItems { get; set; }

        // Display helpers
        public string StatusText => GetStatusText();
        public string ReleaseTypeText => GetReleaseTypeText();
        public string ReleasedByDisplay => !string.IsNullOrEmpty(ReleasedByFullName) ? $"{ReleasedBy} ({ReleasedByFullName})" : ReleasedBy;
        public string ActualReleasedByDisplay => ActualReleasedBy ?? "-";

        // For permissions
        public bool HasEditAccess { get; set; }

        private string GetStatusText()
        {
            if (IsCompleted)
                return "Completed";
            if (IsPartiallyReleased)
                return "Partially Released";
            if (ReleaseDate.Date <= DateTime.UtcNow.Date)
                return "Due for Release";
            return "Scheduled";
        }

        private string GetReleaseTypeText()
        {
            var parts = new List<string>();
            if (EntirePalletCount > 0)
                parts.Add($"{EntirePalletCount} Entire Pallet(s)");
            if (IndividualItemCount > 0)
                parts.Add($"{IndividualItemCount} Individual Item(s)");
            return string.Join(", ", parts);
        }
    }
}
