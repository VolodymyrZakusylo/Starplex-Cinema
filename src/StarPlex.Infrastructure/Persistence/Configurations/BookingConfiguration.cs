using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarPlex.Domain.Entities;

namespace StarPlex.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(b => b.Status).IsRequired();

        builder.HasOne(b => b.Session)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}