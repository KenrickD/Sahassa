using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePalletItem
{
    public class FG_ReceivePalletItemDetailsDto
    {
        public Guid Id { get; set; }
        [MaxLength(100)]
        public string ItemCode { get; set; } = default!;
        public string? BatchNo { get; set; }
        public DateTime? ProdDate { get; set; }
        public string? Remarks { get; set; }
        public bool IsReleased { get; set; }
        public FG_ReceivePalletDetailsDto? FG_ReceivePallet { get; set; }
    }
}
