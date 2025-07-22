using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Models;

namespace WMS.Infrastructure.Data.Configurations
{
    public class FileUploadItemConfiguration : IEntityTypeConfiguration<FileUploadItem>
    {
        public void Configure(EntityTypeBuilder<FileUploadItem> builder)
        {
            builder.ToTable("TB_FileUploadItems");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Reference)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(f => f.FileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(f => f.S3Key)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(f => f.FileType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(f => f.FileSizeBytes)
                .IsRequired();

            builder.Property(f => f.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            // Foreign key relationship
            builder.HasOne(f => f.FileUpload)
                .WithMany(fu => fu.FileUploadItems)
                .HasForeignKey(f => f.FileUploadId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            builder.HasIndex(f => f.FileUploadId);
            builder.HasIndex(f => f.FileType);
            builder.HasIndex(f => f.S3Key).IsUnique(); // S3 keys should be unique
        }
    }
}
