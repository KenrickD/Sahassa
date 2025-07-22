using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.DTOs.GIV_Container
{
    public class ContainerPhotoDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public FileType FileType { get; set; }
        public long? FileSizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string S3Key { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty; 
    }


}
