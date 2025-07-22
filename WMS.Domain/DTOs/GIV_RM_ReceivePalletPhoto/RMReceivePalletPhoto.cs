using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_ReceiveItemPhoto
{
    public class RMReceivePalletPhoto
    {
        [MaxLength(255)]
        public string PhotoFile { get; set; } = default!;
    }
}
