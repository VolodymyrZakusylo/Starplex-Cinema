using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarPlex.Domain.Entities;

public class CinemaConfiguration : IEntityTypeConfiguration<Cinema>
{
    public void Configure(EntityTypeBuilder<Cinema> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Address).IsRequired().HasMaxLength(500);
        builder.Property(c => c.City).IsRequired().HasMaxLength(100);

        builder.HasIndex(c => new { c.Name, c.City })
            .HasMethod("btree")
            .HasAnnotation("Npgsql:IndexOperators", new[] { "text_ops", "text_ops" })
            .IsUnique();

        builder.HasMany(c => c.Halls)
            .WithOne(h => h.Cinema)
            .HasForeignKey(h => h.CinemaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}