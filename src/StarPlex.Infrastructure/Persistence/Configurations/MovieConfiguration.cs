using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarPlex.Domain.Entities;

namespace StarPlex.Infrastructure.Persistence.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).IsRequired().HasMaxLength(250);
        builder.Property(m => m.Slug).IsRequired().HasMaxLength(250);
        builder.Property(m => m.PosterUrl).HasMaxLength(500);
        builder.Property(m => m.BackdropUrl).HasMaxLength(500);
        builder.Property(m => m.PosterStoragePath).HasMaxLength(500);
        builder.Property(m => m.Genre).HasMaxLength(100);
        builder.Property(m => m.TrailerUrl).HasMaxLength(500);
        builder.Property(m => m.AgeRating).HasMaxLength(10);
        builder.Property(m => m.Status).IsRequired();

        builder.HasMany(m => m.Sessions)
            .WithOne(s => s.Movie)
            .HasForeignKey(s => s.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}