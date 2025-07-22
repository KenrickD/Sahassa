using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto.Web
{
    public class RM_ReceivePalletPhotoCreateWebDto
    {
        public string PhotoFile { get; set; } = default!; 
        public string? FileName { get; set; }
        public FileType FileType { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? ContentType { get; set; }
    }
}
