using StarPlex.Domain.Common;
using StarPlex.Domain.Enums;

namespace StarPlex.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid SessionId { get; private set; }
    public decimal TotalPrice { get; set; }
    public DateTime BookingTime { get; private set; }
    public BookingStatus Status { get; set; }
    public Guid? DiscountId { get; set; }
    public Discount? Discount { get; private set; }

    public Session Session { get; private set; } = null!;
    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
    public Payment? Payment { get; private set; }

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

    public bool CanCancel(DateTime requestTime)
    {
        if (Session != null && requestTime >= Session.StartTime.AddMinutes(-60)) return false;
        return true;
    }

    public void CancelIfEmpty(int remainingSeatsCount)
    {
        if (remainingSeatsCount == 0)
        {
            Status = BookingStatus.Cancelled;
        }
    }
}