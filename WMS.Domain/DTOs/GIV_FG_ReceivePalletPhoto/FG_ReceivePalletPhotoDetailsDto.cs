using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto
{
    public class FG_ReceivePalletPhotoDetailsDto
    {
        public Guid Id { get; set; }
        public string PhotoFile { get; set; } = default!;
        public RM_ReceivePalletDetailsDto? FG_ReceivePallet { get; set; }
    }
}
