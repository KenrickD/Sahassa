using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.AWS;

namespace WMS.Application.Helpers
{
    public class AWSHelper
    {
        public AWSConfigDto AWSConfig { get; }
        public AWSHelper(AWSConfigDto aWSConfig)
        {
            this.AWSConfig = aWSConfig;
        }

        public void UploadFile(string sourcePath, string destinationKey)
        {
            //string bucketName = "ntl-app";
            string bucketName = AWSConfig.BucketName;
            string keyName = destinationKey;

            // Set up your AWS credentials
            BasicAWSCredentials credentials = new BasicAWSCredentials(AWSConfig.AccessKey, AWSConfig.SecretKey);

            try
            {
                var config = new Amazon.S3.AmazonS3Config
                {
                    //ServiceURL = "https://s3-ap-southeast-1.amazonaws.com" // Use the correct endpoint for your region
                    ServiceURL = AWSConfig.ServiceURL // Use the correct endpoint for your region
                };
                using (AmazonS3Client s3Client = new AmazonS3Client(credentials, config))
                {
                    // Perform S3 operations
                    // Upload the file to Amazon S3
                    TransferUtility fileTransferUtility = new TransferUtility(s3Client);

                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = bucketName,
                        FilePath = sourcePath,
                        Key = destinationKey
                    };

                    // Set the object-level ACL to public read
                    uploadRequest.CannedACL = S3CannedACL.PublicRead;
                    fileTransferUtility.Upload(uploadRequest);
                }
            }
            catch (AmazonS3Exception e)
            {
                throw new Exception(string.Format("Error encountered on server. Message:'{0}' when writing an object", e.Message));
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("ErrorUnknown encountered on server. Message:'{0}' when writing an object", e.Message));
            }
        }

        //public void UploadPhotos(List<PhotoDto> photos)
        //{
        //    //string bucketName = "ntl-app";
        //    string bucketName = AWSConfig.BucketName;
        //    //string keyName = destinationKey;

        //    // Set up your AWS credentials
        //    BasicAWSCredentials credentials = new BasicAWSCredentials(AWSConfig.AccessKey, AWSConfig.SecretKey);

        //    try
        //    {
        //        var config = new Amazon.S3.AmazonS3Config
        //        {
        //            //ServiceURL = "https://s3-ap-southeast-1.amazonaws.com" // Use the correct endpoint for your region
        //            ServiceURL = AWSConfig.ServiceURL // Use the correct endpoint for your region
        //        };
        //        using (AmazonS3Client s3Client = new AmazonS3Client(credentials, config))
        //        {
        //            // Perform S3 operations
        //            foreach (PhotoDto photo in photos)
        //            {
        //                //// Upload the file to Amazon S3
        //                using (var memoryStream = new MemoryStream())
        //                {
        //                    photo.Image!.CopyTo(memoryStream);

        //                    TransferUtility fileTransferUtility = new TransferUtility(s3Client);

        //                    var uploadRequest = new TransferUtilityUploadRequest
        //                    {
        //                        InputStream = memoryStream,
        //                        BucketName = bucketName,
        //                        Key = photo.AWSPhotoPath
        //                    };

        //                    // Set the object-level ACL to public read
        //                    uploadRequest.CannedACL = S3CannedACL.PublicRead;
        //                    fileTransferUtility.Upload(uploadRequest);
        //                }

        //            }
        //        }
        //    }
        //    catch (AmazonS3Exception e)
        //    {
        //        throw new Exception(string.Format("Error encountered on server. Message:'{0}' when writing an object", e.Message));
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(string.Format("ErrorUnknown encountered on server. Message:'{0}' when writing an object", e.Message));
        //    }
        //}
        //public void UploadAttachmentFiles(List<AttachmentDto> attachmentFiles)
        //{
        //    string bucketName = AWSConfig.BucketName;

        //    // Set up your AWS credentials
        //    BasicAWSCredentials credentials = new BasicAWSCredentials(AWSConfig.AccessKey, AWSConfig.SecretKey);

        //    try
        //    {
        //        var config = new Amazon.S3.AmazonS3Config
        //        {
        //            //ServiceURL = "https://s3-ap-southeast-1.amazonaws.com" // Use the correct endpoint for your region
        //            ServiceURL = AWSConfig.ServiceURL // Use the correct endpoint for your region
        //        };
        //        using (AmazonS3Client s3Client = new AmazonS3Client(credentials, config))
        //        {
        //            // Perform S3 operations
        //            foreach (AttachmentDto file in attachmentFiles)
        //            {
        //                //// Upload the file to Amazon S3
        //                using (var memoryStream = new MemoryStream())
        //                {
        //                    file.AttachmentFile!.CopyTo(memoryStream);

        //                    TransferUtility fileTransferUtility = new TransferUtility(s3Client);

        //                    var uploadRequest = new TransferUtilityUploadRequest
        //                    {
        //                        InputStream = memoryStream,
        //                        BucketName = bucketName,
        //                        Key = file.AWSAttachmentPath
        //                    };

        //                    // Set the object-level ACL to public read
        //                    uploadRequest.CannedACL = S3CannedACL.PublicRead;
        //                    fileTransferUtility.Upload(uploadRequest);
        //                }

        //            }
        //        }
        //    }
        //    catch (AmazonS3Exception e)
        //    {
        //        throw new Exception(string.Format("Error encountered on server. Message:'{0}' when writing an object", e.Message));
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(string.Format("ErrorUnknown encountered on server. Message:'{0}' when writing an object", e.Message));
        //    }
        //}
        public async Task UploadFileAsync(string destinationKey, IFormFile file)
        {
            //string bucketName = "ntl-app";
            string bucketName = AWSConfig.BucketName;
            string keyName = destinationKey;

            // Set up your AWS credentials
            BasicAWSCredentials credentials = new BasicAWSCredentials(AWSConfig.AccessKey, AWSConfig.SecretKey);

            try
            {
                var config = new Amazon.S3.AmazonS3Config
                {
                    //ServiceURL = "https://s3-ap-southeast-1.amazonaws.com" // Use the correct endpoint for your region
                    ServiceURL = AWSConfig.ServiceURL // Use the correct endpoint for your region
                };
                using (AmazonS3Client s3Client = new AmazonS3Client(credentials, config))
                {
                    // Perform S3 operations
                    // Upload the file to Amazon S3
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        TransferUtility fileTransferUtility = new TransferUtility(s3Client);

                        var uploadRequest = new TransferUtilityUploadRequest
                        {
                            InputStream = memoryStream,
                            BucketName = bucketName,
                            Key = destinationKey
                        };

                        // Set the object-level ACL to public read
                        uploadRequest.CannedACL = S3CannedACL.PublicRead;
                        fileTransferUtility.Upload(uploadRequest);
                    }

                }
            }
            catch (AmazonS3Exception e)
            {
                //Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
                throw new Exception(string.Format("Error encountered on server. Message:'{0}' when writing an object", e.Message));
            }
            catch (Exception e)
            {
                //Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                throw new Exception(string.Format("ErrorUnknown encountered on server. Message:'{0}' when writing an object", e.Message));
            }
        }

        public async Task RenameFile(string sourceKey, string destinationKey)
        {
            string sourceBucketName = AWSConfig.BucketName;
            // Set up your AWS credentials
            BasicAWSCredentials credentials = new BasicAWSCredentials(AWSConfig.AccessKey, AWSConfig.SecretKey);
            try
            {
                var config = new Amazon.S3.AmazonS3Config
                {
                    //ServiceURL = "https://s3-ap-southeast-1.amazonaws.com" // Use the correct endpoint for your region
                    ServiceURL = AWSConfig.ServiceURL // Use the correct endpoint for your region
                };
                using (AmazonS3Client s3Client = new AmazonS3Client(credentials, config))
                {
                    // Perform S3 operations
                    // Step 1: Copy the object with the new name
                    var copyRequest = new CopyObjectRequest
                    {
                        SourceBucket = sourceBucketName,
                        SourceKey = sourceKey,
                        DestinationBucket = sourceBucketName,
                        DestinationKey = destinationKey
                    };
                    copyRequest.CannedACL = S3CannedACL.PublicRead;
                    await s3Client.CopyObjectAsync(copyRequest);

                    // Step 2: Delete the original object
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = sourceBucketName,
                        Key = sourceKey
                    };
                    await s3Client.DeleteObjectAsync(deleteRequest);
                }
            }
            catch (AmazonS3Exception e)
            {
                //Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
                throw new Exception(string.Format("Error encountered on server. Message:'{0}' when writing an object", e.Message));
            }
            catch (Exception e)
            {
                //Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                throw new Exception(string.Format("ErrorUnknown encountered on server. Message:'{0}' when writing an object", e.Message));
            }
        }
        public async Task RenameFiles(Dictionary<string, string> keys)
        {
            string sourceBucketName = AWSConfig.BucketName;
            // Set up your AWS credentials
            BasicAWSCredentials credentials = new BasicAWSCredentials(AWSConfig.AccessKey, AWSConfig.SecretKey);
            try
            {
                var config = new Amazon.S3.AmazonS3Config
                {
                    //ServiceURL = "https://s3-ap-southeast-1.amazonaws.com" // Use the correct endpoint for your region
                    ServiceURL = AWSConfig.ServiceURL // Use the correct endpoint for your region
                };
                using (AmazonS3Client s3Client = new AmazonS3Client(credentials, config))
                {
                    // Perform S3 operations
                    foreach (var key in keys)
                    {
                        // Step 1: Copy the object with the new name
                        var copyRequest = new CopyObjectRequest
                        {
                            SourceBucket = sourceBucketName,
                            SourceKey = key.Key,
                            DestinationBucket = sourceBucketName,
                            DestinationKey = key.Value
                        };
                        copyRequest.CannedACL = S3CannedACL.PublicRead;
                        await s3Client.CopyObjectAsync(copyRequest);

                        // Step 2: Delete the original object
                        var deleteRequest = new DeleteObjectRequest
                        {
                            BucketName = sourceBucketName,
                            Key = key.Key
                        };
                        await s3Client.DeleteObjectAsync(deleteRequest);
                    }

                }
            }
            catch (AmazonS3Exception e)
            {
                //Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
                throw new Exception(string.Format("Error encountered on server. Message:'{0}' when writing an object", e.Message));
            }
            catch (Exception e)
            {
                //Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                throw new Exception(string.Format("ErrorUnknown encountered on server. Message:'{0}' when writing an object", e.Message));
            }
        }
        public async Task DeleteFile(string sourceKey)
        {
            string sourceBucketName = AWSConfig.BucketName;
            // Set up your AWS credentials
            BasicAWSCredentials credentials = new BasicAWSCredentials(AWSConfig.AccessKey, AWSConfig.SecretKey);
            try
            {
                var config = new Amazon.S3.AmazonS3Config
                {
                    //ServiceURL = "https://s3-ap-southeast-1.amazonaws.com" // Use the correct endpoint for your region
                    ServiceURL = AWSConfig.ServiceURL // Use the correct endpoint for your region
                };
                using (AmazonS3Client s3Client = new AmazonS3Client(credentials, config))
                {
                    // Perform S3 operations
                    // Delete the original object
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = sourceBucketName,
                        Key = sourceKey
                    };
                    await s3Client.DeleteObjectAsync(deleteRequest);
                }
            }
            catch (AmazonS3Exception e)
            {
                throw new Exception(string.Format("Error encountered on server. Message:'{0}' when deleting an object", e.Message));
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("ErrorUnknown encountered on server. Message:'{0}' when deleting an object", e.Message));
            }
        }
        /// <summary>
        /// Download a single file from an S3 bucket to the local computer.
        /// </summary>
        /// <param name="transferUtil">The transfer initialized TransferUtility
        /// object.</param>
        /// <param name="bucketName">The name of the S3 bucket containing the
        /// file to download.</param>
        /// <param name="keyName">The name of the file to download.</param>
        /// <param name="localPath">The path on the local computer where the
        /// downloaded file will be saved.</param>
        /// <returns>A Boolean value indicating the results of the action.</returns>
        //public async Task<bool> DownloadFilesAsync(List<PhotoDto> photos)
        //{
        //    //string bucketName = "ntl-app";
        //    string bucketName = AWSConfig.BucketName;

        //    // Set up your AWS credentials
        //    BasicAWSCredentials credentials = new BasicAWSCredentials(AWSConfig.AccessKey, AWSConfig.SecretKey);

        //    try
        //    {
        //        var config = new Amazon.S3.AmazonS3Config
        //        {
        //            //ServiceURL = "https://s3-ap-southeast-1.amazonaws.com" // Use the correct endpoint for your region
        //            ServiceURL = AWSConfig.ServiceURL // Use the correct endpoint for your region
        //        };
        //        int countUpload = 0;
        //        using (AmazonS3Client s3Client = new AmazonS3Client(credentials, config))
        //        {
        //            TransferUtility transferUtility = new TransferUtility(s3Client);
        //            foreach (PhotoDto photo in photos)
        //            {
        //                await transferUtility.DownloadAsync(new TransferUtilityDownloadRequest
        //                {
        //                    BucketName = bucketName,
        //                    Key = photo.AWSKeyPath,
        //                    FilePath = photo.LocalPhotoPath,
        //                });
        //                countUpload++;
        //            }
        //        }

        //        return countUpload == photos.Count;
        //    }
        //    catch (AmazonS3Exception e)
        //    {
        //        throw new Exception(string.Format("Error encountered on server. Message:'{0}' when downloading an object", e.Message));
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(string.Format("ErrorUnknown encountered on server. Message:'{0}' when downloading an object", e.Message));
        //    }
        //}
        //public async Task<bool> DownloadFilesAsync(List<AttachmentDto> attachments)
        //{
        //    //string bucketName = "ntl-app";
        //    string bucketName = AWSConfig.BucketName;

        //    // Set up your AWS credentials
        //    BasicAWSCredentials credentials = new BasicAWSCredentials(AWSConfig.AccessKey, AWSConfig.SecretKey);

        //    try
        //    {
        //        var config = new Amazon.S3.AmazonS3Config
        //        {
        //            //ServiceURL = "https://s3-ap-southeast-1.amazonaws.com" // Use the correct endpoint for your region
        //            ServiceURL = AWSConfig.ServiceURL // Use the correct endpoint for your region
        //        };
        //        int countDownload = 0;
        //        using (AmazonS3Client s3Client = new AmazonS3Client(credentials, config))
        //        {
        //            TransferUtility transferUtility = new TransferUtility(s3Client);
        //            foreach (AttachmentDto attachment in attachments)
        //            {
        //                await transferUtility.DownloadAsync(new TransferUtilityDownloadRequest
        //                {
        //                    BucketName = bucketName,
        //                    Key = attachment.AWSKeyPath,
        //                    FilePath = attachment.LocalAttachmentPath,
        //                });
        //                countDownload++;
        //            }
        //        }

        //        return countDownload == attachments.Count;
        //    }
        //    catch (AmazonS3Exception e)
        //    {
        //        throw new Exception(string.Format("Error encountered on server. Message:'{0}' when downloading an object", e.Message));
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(string.Format("ErrorUnknown encountered on server. Message:'{0}' when downloading an object", e.Message));
        //    }
        //}
        public static void ValidateAmazonConfig(string? accessKey, string? secretKey, string? bucketName, string? folderEnv, string? serviceURL)
        {
            if (accessKey == null)
            {
                throw new Exception("Missing AWS Access Key. Not found in config.");
            }
            if (secretKey == null)
            {
                throw new Exception("Missing AWS Secret Key. Not found in config.");
            }
            if (bucketName == null)
            {
                throw new Exception("Missing AWS Bucket Name. Not found in config.");
            }
            if (folderEnv == null)
            {
                throw new Exception("Missing AWS Environment Folder Name. Not found in config.");
            }
            if (serviceURL == null)
            {
                throw new Exception("Missing AWS S3 Service URL. Not found in config.");
            }
        }
        public static string GetCombinedKey(string folderEnvironemnt, string processType, string containerNumber, string fileName)
        {
            return string.Format("{0}/{1}/{2}/{3}", folderEnvironemnt, processType, containerNumber, fileName);
        }
        public static string GetCombinedKeyFolder(string folderEnvironemnt, string processType, string containerNumber)
        {
            return string.Format("{0}/{1}/{2}", folderEnvironemnt, processType, containerNumber);
        }
        public static string GetCombinedFileURL(string serviceURL, string bucketName, string key)
        {
            return string.Format("{0}/{1}/{2}", serviceURL, bucketName, key);
        }

    }
}
