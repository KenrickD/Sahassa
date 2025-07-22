using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePalletItem
{
    public class RM_PalletItemReleaseDto
    {
        public Guid Id { get; set; }
        public string ItemCode { get; set; } = default!;
        public bool IsReleased { get; set; }
    }
}
