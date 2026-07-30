using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarPlex.Domain.Entities;

namespace StarPlex.Infrastructure.Persistence.Configurations;

public class HallConfiguration : IEntityTypeConfiguration<Hall>
{
    public void Configure(EntityTypeBuilder<Hall> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.TotalRows)
            .IsRequired();

        builder.Property(h => h.SeatsPerRow)
            .IsRequired();

        builder.Property(h => h.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Ignore(h => h.TotalCapacity);

        builder.HasIndex(h => new { h.CinemaId, h.Name })
            .IsUnique();

        builder.HasOne(h => h.Cinema)
            .WithMany(c => c.Halls)
            .HasForeignKey(h => h.CinemaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.Seats)
            .WithOne(s => s.Hall)
            .HasForeignKey(s => s.HallId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}