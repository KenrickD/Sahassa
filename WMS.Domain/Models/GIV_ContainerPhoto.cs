using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_ContainerPhoto")]
    public class GIV_ContainerPhoto : BaseEntity
    {
        public Guid ContainerId { get; set; }
        public string PhotoFile { get; set; } = default!;
        public string? FileName { get; set; }
        public FileType FileType { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? ContentType { get; set; }
        public GIV_Container Container { get; set; } = default!;
    }
}
