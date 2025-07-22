using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto
{
    public class RM_ReceivePalletPhotoUploadDto
    {
        public string PalletCode { get; set; } = default!;
        public IFormFile PhotoFile { get; set; } = default!;
    }
}
