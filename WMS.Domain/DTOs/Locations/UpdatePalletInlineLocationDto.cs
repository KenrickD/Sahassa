using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.Locations
{
    public class UpdatePalletInlineLocationDto
    {
        public Guid PalletId { get; set; }
        public Guid LocationId { get; set; }
    }
}
