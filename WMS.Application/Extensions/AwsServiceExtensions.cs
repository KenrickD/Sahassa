using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.AWS;

namespace WMS.Application.Extensions
{
    public static class AwsServiceExtensions
    {
        public static IServiceCollection AddAwsS3Service(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure AWS S3 settings
            services.Configure<AwsS3Config>(configuration.GetSection("AWS:S3"));

            // Register AWS S3 client - SDK automatically handles credential resolution
            // This works without the AWS extensions package
            services.AddScoped<IAmazonS3>(serviceProvider =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<AwsS3Config>>().Value;

                // Create S3 config
                var s3Config = new AmazonS3Config
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(config.Region)
                };

                // For local development, AWS SDK will automatically use:
                // 1. Environment variables (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)
                // 2. AWS CLI credentials (~/.aws/credentials)
                // 3. IAM roles (for EC2)

                return new AmazonS3Client(s3Config);
            });

            // Register our S3 service
            services.AddScoped<IAwsS3Service, AwsS3Service>();

            return services;
        }
    }
}
