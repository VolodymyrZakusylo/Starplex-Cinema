using StarPlex.Domain.Common;

namespace StarPlex.Domain.Entities;

public class BookingSeat : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid SeatId { get; set; }
    public decimal PurchasePrice { get; private set; }

    public Booking Booking { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;

    public BookingSeat()
    {
    }

    public BookingSeat(Guid bookingId, Guid seatId, decimal purchasePrice)
    {
        BookingId = bookingId;
        SeatId = seatId;
        PurchasePrice = purchasePrice;
    }

    public void SetPurchasePrice(decimal purchasePrice)
    {
        PurchasePrice = purchasePrice;
    }
}