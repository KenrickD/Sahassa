using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Models;

namespace WMS.Infrastructure.Data.Configurations
{
    public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
    {
        public void Configure(EntityTypeBuilder<UserClaim> builder)
        {
            builder.HasKey(uc => uc.Id);

            builder.HasIndex(uc => uc.UserId);
            builder.HasIndex(uc => new { uc.UserId, uc.ClaimType });

            builder.Property(uc => uc.ClaimType)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(uc => uc.ClaimValue)
                .HasMaxLength(256);

            builder.HasOne(uc => uc.User)
                .WithMany(u => u.UserClaims)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}