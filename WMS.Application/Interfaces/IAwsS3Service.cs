using Microsoft.AspNetCore.Http;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Application.Interfaces
{
    public interface IAwsS3Service
    {
        // Single file operations
        Task<string> UploadFileAsync(IFormFile file, string? customKey = null);
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? customKey = null);
        Task RenameObjectAsync(string sourceKey, string destinationKey);
        Task DeleteObjectAsync(string key);

        // Batch operations
        Task<Dictionary<string, string>> UploadMultipleFilesAsync(IEnumerable<IFormFile> files, string? keyPrefix = null);
        Task RenameMultipleObjectsAsync(Dictionary<string, string> keyMappings);
        Task DeleteMultipleObjectsAsync(IEnumerable<string> keys);

        // Signed URL generation
        Task<string> GeneratePresignedUrlAsync(string key, FileType fileType);
        Task<string> GeneratePresignedUrlAsync(string key, TimeSpan expiration);
        Task<Dictionary<string, string>> GenerateMultiplePresignedUrlsAsync(IEnumerable<string> keys, FileType fileType);

        // Utility methods
        Task<bool> ObjectExistsAsync(string key);
        Task<long> GetObjectSizeAsync(string key);
        FileType DetermineFileType(string fileName);
        FileType DetermineFileType(IFormFile file);
    }
}
