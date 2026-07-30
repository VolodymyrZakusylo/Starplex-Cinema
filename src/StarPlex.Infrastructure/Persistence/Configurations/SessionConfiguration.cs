using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarPlex.Domain.Entities;

namespace StarPlex.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.BasePrice).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Status).IsRequired();

        builder.Ignore(s => s.EndTime);

        builder.HasOne(s => s.Movie)
            .WithMany(m => m.Sessions)
            .HasForeignKey(s => s.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Hall)
            .WithMany()
            .HasForeignKey(s => s.HallId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Bookings)
            .WithOne(b => b.Session)
            .HasForeignKey(b => b.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.SelectedSeats)
            .WithOne(ss => ss.Session)
            .HasForeignKey(ss => ss.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.HallId, s.StartTime });
        builder.HasIndex(s => new { s.Status, s.StartTime });
    }
}