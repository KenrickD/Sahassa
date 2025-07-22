using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePalletItem
{
    public class RM_ReceivePalletItemDetailsDto
    {
        public Guid Id { get; set; }
        [MaxLength(100)]
        public string ItemCode { get; set; } = default!;
        public string? BatchNo { get; set; }
        public DateTime? ProdDate { get;set; }

        public string? Remarks { get; set; }
        public bool IsReleased { get; set; } = false;
        public RM_ReceivePalletDetailsDto? RM_ReceivePallet { get; set; }

    }
}
