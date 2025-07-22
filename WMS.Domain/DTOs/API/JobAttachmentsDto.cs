namespace WMS.Domain.DTOs.API
{
    public class JobAttachmentsDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string DownloadSignedUrl { get; set; } = string.Empty;
        public string AttachmentType { get; set; } = string.Empty;
        public string AttachmentReference { get; set; } = string.Empty;
    }
}
