using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class BatchMaterialConflictCheckRequest
    {
        public List<Guid> MaterialIds { get; set; } = new List<Guid>();
    }
}
