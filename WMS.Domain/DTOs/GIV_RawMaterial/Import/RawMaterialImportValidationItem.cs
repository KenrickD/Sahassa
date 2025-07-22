using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Import
{
    public class RawMaterialImportValidationItem
    {
        public int RowNumber { get; set; }
        public bool IsValid { get; set; }
        public bool IsUpdate { get; set; } = false;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        // RawMaterial
        public string MaterialNo { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Receive
        public string? BatchNo { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string? ReceivedBy { get; set; }
        public string? ReceiveRemarks { get; set; }
        public TransportType? TypeID { get; set; }

        // Pallet
        public string? PalletCode { get; set; }
        public string? HandledBy { get; set; }
        public Guid? LocationId { get; set; }
        public string? StoredBy { get; set; }
        public int? PackSize { get; set; }

        // Pallet Item
        public string? ItemCode { get; set; }
        public string? ItemBatchNo { get; set; }
        public DateTime? ProdDate { get; set; }
        public bool? DG { get; set; }
        public string? ItemRemarks { get; set; }

        // Photo
        public string? PhotoFile { get; set; }
    }
}
