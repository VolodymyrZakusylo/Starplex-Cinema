using MediatR;

namespace StarPlex.Application.Features.Bookings.Commands.ConfirmBookingFromWebhook;

public class ConfirmBookingFromWebhookCommand : IRequest<bool>
{
    public Guid BookingId { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}
