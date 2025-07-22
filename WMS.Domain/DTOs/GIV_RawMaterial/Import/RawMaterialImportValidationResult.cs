using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Import
{
    public class RawMaterialImportValidationResult
    {
        public bool IsValid { get; set; }
        public int TotalRows { get; set; }
        public List<RawMaterialImportValidationItem> ValidItems { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
