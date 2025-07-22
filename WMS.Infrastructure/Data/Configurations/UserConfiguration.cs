using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Models;

namespace WMS.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => new { u.WarehouseId, u.IsActive });
            builder.HasIndex(u => u.ClientId);

            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .HasMaxLength(100);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(u => u.SecurityStamp)
                .HasMaxLength(256);

            builder.HasOne(u => u.Warehouse)
                .WithMany(w => w.Users)
                .HasForeignKey(u => u.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.Client)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.ClientId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}