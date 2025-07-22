using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Models;

namespace WMS.Infrastructure.Data.Configurations
{
    public class FileUploadConfiguration : IEntityTypeConfiguration<FileUpload>
    {
        public void Configure(EntityTypeBuilder<FileUpload> builder)
        {
            builder.ToTable("TB_FileUpload");

            builder.HasKey(f => f.Id);

            // Index for performance
            builder.HasIndex(f => f.CreatedAt);
        }
    }
}
