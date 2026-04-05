using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineMall.Domain.Entities;

namespace OnlineMall.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // Table name
            builder.ToTable("RefreshTokens");

            // Primary key
            builder.HasKey(t => t.Id);

            // Token must be unique
            builder.HasIndex(t => t.Token)
                   .IsUnique();

            // Token is required and has max length
            builder.Property(t => t.Token)
                   .IsRequired()
                   .HasMaxLength(500);

            // UserId is required
            builder.Property(t => t.UserId)
                   .IsRequired();
        }
    }
}
