using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_Invoicing
{
    public class GroupedPalletCountDto
    {
        public string group { get; set; } = default!;
        public int palletCount { get; set; }
    }
}
