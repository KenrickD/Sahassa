using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto
{
    public class FG_ReceivePalletPhotoCreateDto
    {
        public IFormFile PhotoFile { get; set; } = default!;
    }
}
