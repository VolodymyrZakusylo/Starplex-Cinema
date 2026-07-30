using MediatR;

namespace StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;

public class ConfirmBookingCommand : IRequest<bool>
{
    public Guid BookingId { get; set; }
}