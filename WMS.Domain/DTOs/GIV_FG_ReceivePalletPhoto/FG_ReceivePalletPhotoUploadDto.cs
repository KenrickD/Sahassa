using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto
{
    public class FG_ReceivePalletPhotoUploadDto
    {
        public string PalletCode { get; set; } = default!;
        public IFormFile PhotoFile { get; set; } = default!;
    }
}
