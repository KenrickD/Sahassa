using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs.AWS;
using WMS.Domain.Enumerations;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Application.Services
{
    public class AwsS3Service : IAwsS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AwsS3Config _config;
        private readonly ILogger<AwsS3Service> _logger;
        private readonly bool _isProduction;

        // Allowed file types with their extensions and MIME types
        private static readonly Dictionary<FileType, HashSet<string>> AllowedExtensions = new()
        {
            [FileType.Photo] = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
            [FileType.Document] = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".txt" },
            [FileType.Archive] = new(StringComparer.OrdinalIgnoreCase) { ".zip" }
        };

        private static readonly Dictionary<FileType, HashSet<string>> AllowedMimeTypes = new()
        {
            [FileType.Photo] = new(StringComparer.OrdinalIgnoreCase) {
                "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
            },
            [FileType.Document] = new(StringComparer.OrdinalIgnoreCase) {
                "application/pdf", "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "text/plain"
            },
            [FileType.Archive] = new(StringComparer.OrdinalIgnoreCase) {
                "application/zip", "application/x-zip-compressed"
            }
        };

        public AwsS3Service(IAmazonS3 s3Client, IOptions<AwsS3Config> config, ILogger<AwsS3Service> logger)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Detect environment - production uses IAM roles, local uses AWS CLI credentials
            _isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() != "development";

            _logger.LogInformation("AWS S3 Service initialized. Environment: {Environment}, Bucket: {Bucket}",
                _isProduction ? "Production" : "Development", _config.BucketName);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string? customKey = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty", nameof(file));

            _logger.LogDebug("Starting file upload: {FileName}, Size: {FileSize} bytes",
                file.FileName, file.Length);

            // Validate file
            var fileType = ValidateFile(file);

            // Generate S3 key
            var s3Key = customKey ?? GenerateS3Key(file.FileName, fileType);

            try
            {
                using var stream = file.OpenReadStream();
                await UploadStreamAsync(stream, s3Key, file.ContentType, file.Length);

                _logger.LogInformation("Successfully uploaded file: {FileName} -> {S3Key}",
                    file.FileName, s3Key);

                return s3Key;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file: {FileName}", file.FileName);
                throw new Exception($"Failed to upload file '{file.FileName}': {ex.Message}", ex);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? customKey = null)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("Stream is null or empty", nameof(fileStream));

            _logger.LogDebug("Starting stream upload: {FileName}, Size: {StreamLength} bytes",
                fileName, fileStream.Length);

            // Validate file by extension and content type
            var fileType = DetermineFileType(fileName);
            ValidateFileType(fileName, contentType, fileType, fileStream.Length);

            var s3Key = customKey ?? GenerateS3Key(fileName, fileType);

            try
            {
                await UploadStreamAsync(fileStream, s3Key, contentType, fileStream.Length);

                _logger.LogInformation("Successfully uploaded stream: {FileName} -> {S3Key}",
                    fileName, s3Key);

                return s3Key;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload stream: {FileName}", fileName);
                throw new Exception($"Failed to upload stream '{fileName}': {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<string, string>> UploadMultipleFilesAsync(IEnumerable<IFormFile> files, string? keyPrefix = null)
        {
            var results = new Dictionary<string, string>();
            var fileList = files.ToList();

            _logger.LogInformation("Starting batch upload of {FileCount} files", fileList.Count);

            foreach (var file in fileList)
            {
                try
                {
                    var customKey = !string.IsNullOrEmpty(keyPrefix)
                        ? $"{keyPrefix}/{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}"
                        : null;

                    var s3Key = await UploadFileAsync(file, customKey);
                    results[file.FileName] = s3Key;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload file in batch: {FileName}", file.FileName);
                    throw new Exception($"Batch upload failed at file '{file.FileName}': {ex.Message}", ex);
                }
            }

            _logger.LogInformation("Successfully completed batch upload of {FileCount} files", results.Count);
            return results;
        }

        public async Task RenameObjectAsync(string sourceKey, string destinationKey)
        {
            if (string.IsNullOrEmpty(sourceKey))
                throw new ArgumentException("Source key cannot be null or empty", nameof(sourceKey));
            if (string.IsNullOrEmpty(destinationKey))
                throw new ArgumentException("Destination key cannot be null or empty", nameof(destinationKey));

            _logger.LogDebug("Renaming object: {SourceKey} -> {DestinationKey}", sourceKey, destinationKey);

            try
            {
                // Copy object to new key
                var copyRequest = new CopyObjectRequest
                {
                    SourceBucket = _config.BucketName,
                    SourceKey = sourceKey,
                    DestinationBucket = _config.BucketName,
                    DestinationKey = destinationKey,
                    CannedACL = S3CannedACL.Private // Always private since we use signed URLs
                };

                await _s3Client.CopyObjectAsync(copyRequest);

                // Delete original object
                await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = _config.BucketName,
                    Key = sourceKey
                });

                _logger.LogInformation("Successfully renamed object: {SourceKey} -> {DestinationKey}",
                    sourceKey, destinationKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rename object: {SourceKey} -> {DestinationKey}",
                    sourceKey, destinationKey);
                throw new Exception($"Failed to rename object from '{sourceKey}' to '{destinationKey}': {ex.Message}", ex);
            }
        }

        public async Task RenameMultipleObjectsAsync(Dictionary<string, string> keyMappings)
        {
            if (keyMappings == null || !keyMappings.Any())
                throw new ArgumentException("Key mappings cannot be null or empty", nameof(keyMappings));

            _logger.LogInformation("Starting batch rename of {ObjectCount} objects", keyMappings.Count);

            foreach (var mapping in keyMappings)
            {
                await RenameObjectAsync(mapping.Key, mapping.Value);
            }

            _logger.LogInformation("Successfully completed batch rename of {ObjectCount} objects", keyMappings.Count);
        }

        public async Task DeleteObjectAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _logger.LogDebug("Deleting object: {Key}", key);

            try
            {
                await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = _config.BucketName,
                    Key = key
                });

                _logger.LogInformation("Successfully deleted object: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete object: {Key}", key);
                throw new Exception($"Failed to delete object '{key}': {ex.Message}", ex);
            }
        }

        public async Task DeleteMultipleObjectsAsync(IEnumerable<string> keys)
        {
            var keyList = keys.ToList();
            if (!keyList.Any())
                throw new ArgumentException("Keys collection cannot be empty", nameof(keys));

            _logger.LogInformation("Starting batch delete of {ObjectCount} objects", keyList.Count);

            try
            {
                // S3 supports batch delete up to 1000 objects
                const int batchSize = 1000;
                for (int i = 0; i < keyList.Count; i += batchSize)
                {
                    var batch = keyList.Skip(i).Take(batchSize);
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = _config.BucketName,
                        Objects = batch.Select(key => new KeyVersion { Key = key }).ToList()
                    };

                    await _s3Client.DeleteObjectsAsync(deleteRequest);
                }

                _logger.LogInformation("Successfully completed batch delete of {ObjectCount} objects", keyList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete multiple objects");
                throw new Exception($"Failed to delete multiple objects: {ex.Message}", ex);
            }
        }

        public async Task<string> GeneratePresignedUrlAsync(string key, FileType fileType)
        {
            var expiration = fileType switch
            {
                FileType.Photo => _config.PhotoUrlExpiration,
                FileType.Document => _config.DocumentUrlExpiration,
                FileType.Archive => _config.ArchiveUrlExpiration,
                _ => _config.DocumentUrlExpiration
            };

            return await GeneratePresignedUrlAsync(key, expiration);
        }

        public async Task<string> GeneratePresignedUrlAsync(string key, TimeSpan expiration)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _logger.LogDebug("Generating presigned URL for: {Key}, Expiration: {Expiration}",
                key, expiration);

            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _config.BucketName,
                    Key = key,
                    Expires = DateTime.UtcNow.Add(expiration),
                    Verb = HttpVerb.GET
                };

                var url = await _s3Client.GetPreSignedURLAsync(request);

                _logger.LogDebug("Generated presigned URL for: {Key}", key);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate presigned URL for: {Key}", key);
                throw new Exception($"Failed to generate presigned URL for '{key}': {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<string, string>> GenerateMultiplePresignedUrlsAsync(IEnumerable<string> keys, FileType fileType)
        {
            var results = new Dictionary<string, string>();
            var keyList = keys.ToList();

            _logger.LogDebug("Generating {UrlCount} presigned URLs for file type: {FileType}",
                keyList.Count, fileType);

            foreach (var key in keyList)
            {
                var url = await GeneratePresignedUrlAsync(key, fileType);
                results[key] = url;
            }

            _logger.LogDebug("Successfully generated {UrlCount} presigned URLs", results.Count);
            return results;
        }

        public async Task<bool> ObjectExistsAsync(string key)
        {
            try
            {
                await _s3Client.GetObjectMetadataAsync(_config.BucketName, key);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<long> GetObjectSizeAsync(string key)
        {
            try
            {
                var metadata = await _s3Client.GetObjectMetadataAsync(_config.BucketName, key);
                return metadata.ContentLength;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get object size for: {Key}", key);
                throw new Exception($"Failed to get object size for '{key}': {ex.Message}", ex);
            }
        }

        public FileType DetermineFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            foreach (var kvp in AllowedExtensions)
            {
                if (kvp.Value.Contains(extension))
                    return kvp.Key;
            }

            throw new ArgumentException($"Unsupported file type: {extension}");
        }

        public FileType DetermineFileType(IFormFile file)
        {
            return DetermineFileType(file.FileName);
        }

        #region Private Methods

        private FileType ValidateFile(IFormFile file)
        {
            var fileType = DetermineFileType(file);
            ValidateFileType(file.FileName, file.ContentType, fileType, file.Length);
            return fileType;
        }

        private void ValidateFileType(string fileName, string contentType, FileType fileType, long fileSize)
        {
            // Validate extension
            var extension = Path.GetExtension(fileName);
            if (!AllowedExtensions[fileType].Contains(extension))
            {
                throw new ArgumentException($"File extension '{extension}' is not allowed for {fileType} files");
            }

            // Validate MIME type
            //if (!AllowedMimeTypes[fileType].Contains(contentType))
            //{
            //    throw new ArgumentException($"Content type '{contentType}' is not allowed for {fileType} files");
            //}

            // Validate file size
            var maxSize = fileType switch
            {
                FileType.Photo => _config.MaxPhotoSizeBytes,
                FileType.Document => _config.MaxDocumentSizeBytes,
                FileType.Archive => _config.MaxArchiveSizeBytes,
                _ => _config.MaxDocumentSizeBytes
            };

            if (fileSize > maxSize)
            {
                throw new ArgumentException($"File size ({fileSize:N0} bytes) exceeds maximum allowed size for {fileType} files ({maxSize:N0} bytes)");
            }
        }

        private string GenerateS3Key(string fileName, FileType fileType)
        {
            var sanitizedFileName = SanitizeFileName(fileName);
            var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd/HH");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];

            return $"{_config.FolderEnvironment}/{fileType.ToString().ToLower()}/{timestamp}/{uniqueId}_{sanitizedFileName}";
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder();

            foreach (var c in fileName)
            {
                if (!invalidChars.Contains(c))
                    sanitized.Append(c);
                else
                    sanitized.Append('_');
            }

            return sanitized.ToString();
        }

        private async Task UploadStreamAsync(Stream stream, string s3Key, string contentType, long contentLength)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _config.BucketName,
                Key = s3Key,
                InputStream = stream,
                ContentType = contentType,
                CannedACL = S3CannedACL.Private, // Always private since we use signed URLs
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            await _s3Client.PutObjectAsync(putRequest);
        }

        #endregion
    }
}