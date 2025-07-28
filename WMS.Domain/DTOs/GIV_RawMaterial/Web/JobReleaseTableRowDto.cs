using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class JobReleaseTableRowDto
    {
        public Guid JobId { get; set; }
        public DateTime PlannedReleaseDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? CreatedByFullName { get; set; }
        public int MaterialCount { get; set; }
        public int TotalPallets { get; set; }
        public int TotalItems { get; set; }
        public string JobStatus { get; set; } = string.Empty;
        public decimal CompletionPercentage { get; set; }

        // Display helpers
        public string CreatedByDisplay => !string.IsNullOrEmpty(CreatedByFullName)
            ? $"{CreatedBy} ({CreatedByFullName})"
            : CreatedBy;

        public string JobIdShort => JobId.ToString().Substring(0, 8).ToUpper();
        public bool HasDeleteAccess { get; set; } 
    }
}
