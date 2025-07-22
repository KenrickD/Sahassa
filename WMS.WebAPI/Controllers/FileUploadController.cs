

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using WMS.Application.Interfaces;
//using WMS.Domain.Models;
//using WMS.Infrastructure.Data;

//namespace WMS.WebAPI.Controllers
//{
//    public class FileUploadController : ControllerBase
//    {
//        private readonly IAwsS3Service _s3Service;
//        private readonly AppDbContext _context;
//        private readonly ILogger<FileUploadController> _logger;

//        public FileUploadController(
//            IAwsS3Service s3Service,
//            AppDbContext context,
//            ILogger<FileUploadController> logger)
//        {
//            _s3Service = s3Service;
//            _context = context;
//            _logger = logger;
//        }

//        [HttpPost("upload")]
//        public async Task<IActionResult> UploadFiles(
//            [FromForm] IFormFileCollection files,
//            [FromForm] string? reference = null,
//            [FromForm] string? description = null)
//        {
//            if (!files.Any())
//                return BadRequest("No files provided");

//            try
//            {
//                // Create FileUpload record
//                var fileUpload = new FileUpload
//                {
//                    Id = Guid.NewGuid()
//                };

//                _context.FileUploads.Add(fileUpload);
//                await _context.SaveChangesAsync();

//                // Upload files to S3 and create FileUploadItem records
//                var uploadedItems = new List<FileUploadItem>();

//                foreach (var file in files)
//                {
//                    // Upload to S3
//                    var s3Key = await _s3Service.UploadFileAsync(file);
//                    var fileType = _s3Service.DetermineFileType(file);

//                    // Create database record
//                    var fileItem = new FileUploadItem
//                    {
//                        Id = Guid.NewGuid(),
//                        FileUploadId = fileUpload.Id,
//                        FileName = file.FileName,
//                        S3Key = s3Key,
//                        FileType = fileType,
//                        FileSizeBytes = file.Length,
//                        ContentType = file.ContentType
//                    };

//                    uploadedItems.Add(fileItem);
//                }

//                _context.FileUploadItems.AddRange(uploadedItems);

//                // Update status to completed
//                await _context.SaveChangesAsync();

//                _logger.LogInformation("Successfully uploaded {FileCount} files for FileUpload {FileUploadId}",
//                    files.Count, fileUpload.Id);

//                return Ok(new
//                {
//                    fileUploadId = fileUpload.Id,
//                    totalFiles = uploadedItems.Count,
//                    files = uploadedItems.Select(item => new
//                    {
//                        id = item.Id,
//                        fileName = item.FileName,
//                        fileType = item.FileType.ToString(),
//                        fileSizeBytes = item.FileSizeBytes,
//                        s3Key = item.S3Key
//                    })
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to upload files");
//                return StatusCode(500, $"Upload failed: {ex.Message}");
//            }
//        }

//        [HttpGet("{fileUploadId}/files")]
//        public async Task<IActionResult> GetFileUrls(Guid fileUploadId)
//        {
//            try
//            {
//                var fileUpload = await _context.FileUploads
//                    .Include(fu => fu.FileUploadItems)
//                    .FirstOrDefaultAsync(fu => fu.Id == fileUploadId);

//                if (fileUpload == null)
//                    return NotFound($"FileUpload with ID {fileUploadId} not found");

//                var fileUrls = new List<object>();

//                foreach (var item in fileUpload.FileUploadItems.Where(i => !i.IsDeleted))
//                {
//                    var signedUrl = await _s3Service.GeneratePresignedUrlAsync(item.S3Key, item.FileType);

//                    fileUrls.Add(new
//                    {
//                        id = item.Id,
//                        fileName = item.FileName,
//                        fileType = item.FileType.ToString(),
//                        fileSizeBytes = item.FileSizeBytes,
//                        url = signedUrl,
//                        reference = item.Reference
//                    });
//                }

//                return Ok(new
//                {
//                    fileUploadId = fileUpload.Id,
//                    files = fileUrls
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to get file URLs for FileUpload {FileUploadId}", fileUploadId);
//                return StatusCode(500, $"Failed to get file URLs: {ex.Message}");
//            }
//        }

//        [HttpGet("item/{itemId}/url")]
//        public async Task<IActionResult> GetSingleFileUrl(Guid itemId)
//        {
//            try
//            {
//                var fileItem = await _context.FileUploadItems
//                    .FirstOrDefaultAsync(fi => fi.Id == itemId && !fi.IsDeleted);

//                if (fileItem == null)
//                    return NotFound($"FileUploadItem with ID {itemId} not found");

//                var signedUrl = await _s3Service.GeneratePresignedUrlAsync(fileItem.S3Key, fileItem.FileType);

//                return Ok(new
//                {
//                    id = fileItem.Id,
//                    fileName = fileItem.FileName,
//                    fileType = fileItem.FileType.ToString(),
//                    url = signedUrl
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to get file URL for item {ItemId}", itemId);
//                return StatusCode(500, $"Failed to get file URL: {ex.Message}");
//            }
//        }

//        [HttpPut("item/{itemId}/rename")]
//        public async Task<IActionResult> RenameFile(Guid itemId, [FromBody] RenameFileRequest request)
//        {
//            if (string.IsNullOrEmpty(request.NewFileName))
//                return BadRequest("New file name is required");

//            try
//            {
//                var fileItem = await _context.FileUploadItems
//                    .FirstOrDefaultAsync(fi => fi.Id == itemId && !fi.IsDeleted);

//                if (fileItem == null)
//                    return NotFound($"FileUploadItem with ID {itemId} not found");

//                // Generate new S3 key with new filename
//                var fileExtension = Path.GetExtension(fileItem.FileName);
//                var newFileName = request.NewFileName.EndsWith(fileExtension)
//                    ? request.NewFileName
//                    : request.NewFileName + fileExtension;

//                var newS3Key = GenerateNewS3Key(fileItem.S3Key, newFileName);

//                // Rename in S3
//                await _s3Service.RenameObjectAsync(fileItem.S3Key, newS3Key);

//                // Update database
//                fileItem.FileName = newFileName;
//                fileItem.S3Key = newS3Key;
//                fileItem.ModifiedAt = DateTime.UtcNow;

//                await _context.SaveChangesAsync();

//                _logger.LogInformation("Successfully renamed file item {ItemId}: {OldName} -> {NewName}",
//                    itemId, fileItem.FileName, newFileName);

//                return Ok(new
//                {
//                    id = fileItem.Id,
//                    oldFileName = request.NewFileName,
//                    newFileName = fileItem.FileName,
//                    newS3Key = fileItem.S3Key
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to rename file item {ItemId}", itemId);
//                return StatusCode(500, $"Failed to rename file: {ex.Message}");
//            }
//        }

//        [HttpDelete("item/{itemId}")]
//        public async Task<IActionResult> DeleteFile(Guid itemId)
//        {
//            try
//            {
//                var fileItem = await _context.FileUploadItems
//                    .Include(fi => fi.FileUpload)
//                    .FirstOrDefaultAsync(fi => fi.Id == itemId && !fi.IsDeleted);

//                if (fileItem == null)
//                    return NotFound($"FileUploadItem with ID {itemId} not found");

//                // Delete from S3
//                await _s3Service.DeleteObjectAsync(fileItem.S3Key);

//                // Soft delete in database
//                //fileItem.IsActive = false;
//                fileItem.IsDeleted = true;
//                fileItem.ModifiedAt = DateTime.UtcNow;

//                await _context.SaveChangesAsync();

//                _logger.LogInformation("Successfully deleted file item {ItemId}: {FileName}",
//                    itemId, fileItem.FileName);

//                return Ok(new { message = $"File '{fileItem.FileName}' deleted successfully" });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to delete file item {ItemId}", itemId);
//                return StatusCode(500, $"Failed to delete file: {ex.Message}");
//            }
//        }

//        [HttpDelete("{fileUploadId}")]
//        public async Task<IActionResult> DeleteFileUpload(Guid fileUploadId)
//        {
//            try
//            {
//                var fileUpload = await _context.FileUploads
//                    .Include(fu => fu.FileUploadItems)
//                    .FirstOrDefaultAsync(fu => fu.Id == fileUploadId);

//                if (fileUpload == null)
//                    return NotFound($"FileUpload with ID {fileUploadId} not found");

//                // Get all active S3 keys
//                var s3Keys = fileUpload.FileUploadItems
//                    .Where(fi => !fi.IsDeleted)
//                    .Select(fi => fi.S3Key)
//                    .ToList();

//                if (s3Keys.Any())
//                {
//                    // Delete all files from S3
//                    await _s3Service.DeleteMultipleObjectsAsync(s3Keys);
//                }

//                // Soft delete all items
//                foreach (var item in fileUpload.FileUploadItems)
//                {
//                    //item.IsActive = false;
//                    item.IsDeleted = true;
//                    item.ModifiedAt = DateTime.UtcNow;
//                }

//                // Update file upload status
//                fileUpload.IsDeleted = true;
//                fileUpload.ModifiedAt = DateTime.UtcNow;

//                await _context.SaveChangesAsync();

//                _logger.LogInformation("Successfully deleted FileUpload {FileUploadId} with {FileCount} files",
//                    fileUploadId, s3Keys.Count);

//                return Ok(new { message = $"FileUpload and {s3Keys.Count} files deleted successfully" });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to delete FileUpload {FileUploadId}", fileUploadId);
//                return StatusCode(500, $"Failed to delete file upload: {ex.Message}");
//            }
//        }

//        private string GenerateNewS3Key(string currentS3Key, string newFileName)
//        {
//            var pathParts = currentS3Key.Split('/');
//            pathParts[^1] = newFileName; // Replace the last part (filename) with new name
//            return string.Join("/", pathParts);
//        }
//    }

//    // Request DTOs
//    public class RenameFileRequest
//    {
//        public string NewFileName { get; set; } = string.Empty;
//    }
//}
