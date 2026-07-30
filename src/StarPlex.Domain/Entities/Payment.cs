using StarPlex.Domain.Common;
using StarPlex.Domain.Enums;

namespace StarPlex.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }

    public Booking Booking { get; set; } = null!;

    public Payment()
    {
    }

    public Payment(Guid bookingId, string stripePaymentIntentId, decimal amount, PaymentStatus status, DateTime? paidAt = null)
    {
        BookingId = bookingId;
        StripePaymentIntentId = stripePaymentIntentId;
        Amount = amount;
        Status = status;
        PaidAt = paidAt;
    }
}