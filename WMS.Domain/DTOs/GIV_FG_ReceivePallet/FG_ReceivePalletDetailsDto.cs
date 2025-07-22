using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePallet
{
    public class FG_ReceivePalletDetailsDto
    {
        public Guid Id { get; set; }
        public string? PalletCode { get; set; }
        public string HandledBy { get; set; } = default!;
        public string? StoredBy { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? ReceivedBy { get; set; }
        public int PackSize { get; set; }
        public string? LocationName { get; set; }
        public bool IsReleased { get; set; }
        public List<FG_ReceivePalletItemDetailsDto>? FG_ReceivePalletItems { get; set; }
        public List<FG_ReceivePalletPhotoDetailsDto>? FG_ReceivePalletPhotos { get; set; }
        public FG_ReceiveDetailsDto? FG_Receive { get; set; }
        public Location? Location { get; set; }
        public bool HasEditAccess { get; set; }
        public int Quantity { get; set; }
        public int QuantityBalance { get; set; }
    }
}
