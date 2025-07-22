using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto
{
    public class FG_ReceivePalletPhotoAuditDto
    {
        public Guid Id { get; set; }
        public string PhotoFile { get; set; } = default!;
    }

}
