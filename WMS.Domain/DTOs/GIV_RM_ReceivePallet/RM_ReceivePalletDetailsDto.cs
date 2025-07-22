using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_Receive;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePallet
{
    public class RM_ReceivePalletDetailsDto
    {
        public Guid Id { get; set; }
        public string? PalletCode { get; set; }
        public string HandledBy { get; set; } = default!;
        public string? StoredBy { get; set; }
        public int PackSize { get; set; }
        public string? LocationName { get; set; }
        public bool IsReleased { get; set; } = false;
        public int Quantity { get; set; }
        public int QuantityBalance { get; set; }
        public List<RM_ReceivePalletItemDetailsDto>? RM_ReceivePalletItems { get; set; }
        public List<RM_ReceivePalletPhotoDetailsDto>? RM_ReceivePalletPhotos { get; set; }
        public RM_ReceiveDetailsDto? RM_Receive { get; set; }
        public Location? Location { get; set; }
        public bool HasEditAccess { get; set; }
    }
}
