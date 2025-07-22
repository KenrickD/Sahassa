namespace WMS.Domain.DTOs.AWS
{
    public class AWSConfigDto
    {
        public string AccessKey { get; set; } = default!;
        public string SecretKey { get; set; } = default!;
        public string BucketName { get; set; } = default!;
        public string FolderEnvironment { get; set; } = default!;
        public string ServiceURL { get; set; } = default!;
    }
}
