using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class FG_ReleaseDetailsDto
    {
        public Guid Id { get; set; }
        public Guid ReleaseId { get; set; }
        public Guid? ReceiveId { get; set; }
        public Guid PalletId { get; set; } // Use Guid.Empty for missing pallets
        public Guid? ItemId { get; set; }
        public bool IsEntirePallet { get; set; }
        public DateTime? ActualReleaseDate { get; set; }
        public string? ActualReleasedBy { get; set; }

        // Display information
        public string PalletCode { get; set; } = string.Empty;
        public string? ItemCode { get; set; }
        public List<string> AllItemCodes { get; set; } = new List<string>(); // For entire pallet releases
        public string BatchNo { get; set; } = string.Empty;
        public string? LocationCode { get; set; }
        public DateTime ReceivedDate { get; set; }
        public int PackSize { get; set; }

        // Status
        public bool IsActuallyReleased => ActualReleaseDate.HasValue;
        public string StatusText => IsActuallyReleased ? "Released" : "Pending";
        public string StatusClass => IsActuallyReleased ? "status-released" : "status-pending";

        // Type display
        public string TypeText => IsEntirePallet ? "Entire Pallet" : "Individual Item";
        public string ReleaseTarget => IsEntirePallet ? PalletCode : $"{ItemCode} (from {PalletCode})";

        // Release info display
        public string ActualReleaseInfo => IsActuallyReleased
            ? $"{ActualReleasedBy} on {ActualReleaseDate?.ToString("yyyy-MM-dd HH:mm")}"
            : "Not released yet";

        // Item codes display for table
        public string ItemCodesDisplay => IsEntirePallet && AllItemCodes.Any()
            ? string.Join(", ", AllItemCodes)
            : (ItemCode ?? "-");
        public bool HasDeleteAccess { get; set; }
    }
}
