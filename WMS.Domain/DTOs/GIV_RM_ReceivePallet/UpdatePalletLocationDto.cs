using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePallet
{
    public class UpdatePalletLocationDto
    {
        public string? Code { get; set; }
        public string? LocationId { get; set; }
        public string? StoredBy { get; set; }
    }
}
