using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_FinishedGood;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.GIV_FG_Receive
{
    public class FG_ReceiveDetailsDto
    {
        public Guid Id { get; set; }
        public TransportType TypeID { get; set; }
        [MaxLength(100)]
        public string BatchNo { get; set; } = default!;
        public DateTime ReceivedDate { get; set; }
        public string ReceivedBy { get; set; } = default!;
        public string Remarks { get; set; } = default!;
        public FinishedGoodDetailsDto? FinishedGood { get; set; }
        public List<FG_ReceivePalletDetailsDto>? FG_ReceivePallets { get; set; }
    }
}
