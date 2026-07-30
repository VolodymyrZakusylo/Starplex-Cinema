using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarPlex.Domain.Entities;

namespace StarPlex.Infrastructure.Persistence.Configurations;

public class SelectedSeatConfiguration : IEntityTypeConfiguration<SelectedSeat>
{
    public void Configure(EntityTypeBuilder<SelectedSeat> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Session)
            .WithMany(s => s.SelectedSeats)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LockedUntil);

        builder.HasIndex(ss => new { ss.SessionId, ss.SeatId }).IsUnique();
    }
}