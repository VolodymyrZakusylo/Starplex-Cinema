using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarPlex.Domain.Entities;

public class BookingSeatConfiguration : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(EntityTypeBuilder<BookingSeat> builder)
    {
        builder.HasKey(bs => bs.Id);
        builder.HasOne(bs => bs.Booking)
            .WithMany(b => b.BookingSeats)
            .HasForeignKey(bs => bs.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(bs => bs.Seat)
            .WithMany()
            .HasForeignKey(bs => bs.SeatId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(bs => bs.PurchasePrice).HasColumnType("decimal(18,2)");
        builder.HasOne(bs => bs.Ticket)
            .WithOne(t => t.BookingSeat)
            .HasForeignKey<Ticket>(t => t.BookingSeatId);
    }
}