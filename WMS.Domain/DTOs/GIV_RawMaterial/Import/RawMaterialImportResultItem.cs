using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Import
{
    public class RawMaterialImportResultItem
    {
        public int RowNumber { get; set; }
        public string MaterialNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Success, Error, Warning
        public string Message { get; set; } = string.Empty;

        public bool IsRawMaterialInserted { get; set; }
        public bool IsRawMaterialUpdated { get; set; }
        public bool IsReceiveInserted { get; set; }
        public bool IsPalletInserted { get; set; }
        public bool IsItemInserted { get; set; }
        public bool IsPhotoInserted { get; set; }
    }
}
