using StarPlex.Domain.Common;
using StarPlex.Domain.Enums;

namespace StarPlex.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime BookingTime { get; set; }
    public BookingStatus Status { get; set; }
    public Guid? DiscountId { get; set; }
    public Discount? Discount { get; set; }

    public Session Session { get; set; } = null!;
    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
    public Payment? Payment { get; set; }

    public Booking()
    {
    }

    public Booking(Guid userId, Guid sessionId, decimal totalPrice, DateTime bookingTime, BookingStatus status)
    {
        UserId = userId;
        SessionId = sessionId;
        TotalPrice = totalPrice;
        BookingTime = bookingTime;
        Status = status;
    }
}