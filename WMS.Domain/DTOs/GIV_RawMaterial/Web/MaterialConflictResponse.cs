using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class MaterialConflictResponse
    {
        public Dictionary<Guid, ConflictInfo> Pallets { get; set; } = new();
        public Dictionary<Guid, ConflictInfo> Items { get; set; } = new();
    }
}
