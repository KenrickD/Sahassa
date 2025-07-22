using System.ComponentModel.DataAnnotations;


namespace WMS.Domain.DTOs.AWS
{
    public class AwsS3Config
    {
        [Required]
        public string BucketName { get; set; } = string.Empty;

        [Required]
        public string Region { get; set; } = string.Empty;

        [Required]
        public string FolderEnvironment { get; set; } = string.Empty;

        public TimeSpan PhotoUrlExpiration { get; set; } = TimeSpan.FromHours(24);
        public TimeSpan DocumentUrlExpiration { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan ArchiveUrlExpiration { get; set; } = TimeSpan.FromHours(1);

        // File size limits in bytes
        public long MaxPhotoSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
        public long MaxDocumentSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB  
        public long MaxArchiveSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    }
}
