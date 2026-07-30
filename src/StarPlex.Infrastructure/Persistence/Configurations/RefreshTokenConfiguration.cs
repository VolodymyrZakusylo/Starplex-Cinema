using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarPlex.Domain.Entities;

namespace StarPlex.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Token).IsRequired().HasMaxLength(500);
        builder.HasIndex(t => t.Token).IsUnique();
    }
}