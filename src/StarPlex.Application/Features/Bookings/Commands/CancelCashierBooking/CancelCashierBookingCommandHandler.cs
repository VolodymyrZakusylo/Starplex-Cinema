using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StarPlex.Application.Features.Bookings.Commands.CancelCashierBooking;

public class CancelCashierBookingCommandHandler : IRequestHandler<CancelCashierBookingCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ISeatHubService _seatHubService;

    public CancelCashierBookingCommandHandler(IApplicationDbContext context, ISeatHubService seatHubService)
    {
        _context = context;
        _seatHubService = seatHubService;
    }

    public async Task<bool> Handle(CancelCashierBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingSeats)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            return false;
        }

        if (booking.UserId != Guid.Empty)
        {
            throw new InvalidOperationException("Error: Attempted to cancel an online user booking via the cashier method. Please use Stripe cancellation.");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("This cashier booking has already been cancelled.");
        }

        booking.Status = BookingStatus.Cancelled;

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == booking.Id, cancellationToken);

        if (payment != null)
        {
            payment.Status = PaymentStatus.Refunded;
            payment.PaidAt = DateTime.UtcNow;
        }

        var bookingSeatIds = booking.BookingSeats.Select(bs => bs.Id).ToList();
        var tickets = await _context.Tickets
            .Where(t => bookingSeatIds.Contains(t.BookingSeatId))
            .ToListAsync(cancellationToken);

        foreach (var ticket in tickets)
        {
            ticket.IsUsed = false;
        }

        if (booking.BookingSeats.Any())
        {
            _context.BookingSeats.RemoveRange(booking.BookingSeats);
        }

        var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
        var ghostLocks = await _context.SelectedSeats
            .Where(ss => ss.SessionId == booking.SessionId && seatIds.Contains(ss.SeatId))
            .ToListAsync(cancellationToken);

        if (ghostLocks.Any())
        {
            _context.SelectedSeats.RemoveRange(ghostLocks);
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (seatIds.Any())
        {
            await _seatHubService.NotifySeatsReleasedAsync(booking.SessionId, seatIds, cancellationToken);
        }

        return true;
    }
}