using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class JobReleaseDetailsDto
    {
        public Guid JobId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? CreatedByFullName { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public int FinishedGoodCount { get; set; }
        public int TotalPallets { get; set; }
        public int TotalItems { get; set; }
        public decimal CompletionPercentage { get; set; }
        public DateTime PlannedReleaseDate { get; set; }

        // Display helpers
        public string CreatedByDisplay => !string.IsNullOrEmpty(CreatedByFullName)
            ? $"{CreatedBy} ({CreatedByFullName})"
            : CreatedBy;

        public string JobIdShort => JobId.ToString().Substring(0, 8).ToUpper();

        public string CompletionDisplay => $"{CompletionPercentage:F1}%";
    }
}
