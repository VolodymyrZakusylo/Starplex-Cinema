using MediatR;
using System;

namespace StarPlex.Application.Features.Bookings.Commands.CancelCashierBooking;

public class CancelCashierBookingCommand : IRequest<bool>
{
    public Guid BookingId { get; set; }
}