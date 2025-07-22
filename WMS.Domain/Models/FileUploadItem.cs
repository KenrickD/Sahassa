
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.Models
{
    [Table("TB_FileUploadItem")]
    public class FileUploadItem : BaseEntity
    {
        public Guid FileUploadId { get; set; }
        public virtual FileUpload FileUpload { get; set; } = null!;

        [MaxLength(500)]
        public string? Reference { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string S3Key { get; set; } = string.Empty;

        public FileType FileType { get; set; }

        public long FileSizeBytes { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        //public bool IsActive { get; set; } = true;
    }
}
