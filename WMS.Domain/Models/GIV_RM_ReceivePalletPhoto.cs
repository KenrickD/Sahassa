using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_RM_ReceivePalletPhoto")]
    public class GIV_RM_ReceivePalletPhoto :TenantEntity
    {
        public virtual GIV_RM_ReceivePallet GIV_RM_ReceivePallet { get; set; } = default!;
        public Guid GIV_RM_ReceivePalletId { get; set; }
        public string PhotoFile { get; set; } = default!;
        public string? FileName { get; set; }
        public FileType FileType { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? ContentType { get; set; }
    }
}
