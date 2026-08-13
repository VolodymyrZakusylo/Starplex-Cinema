using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Halls.Commands.UpdateSeatProperties;

public class UpdateSeatPropertiesCommandHandler : IRequestHandler<UpdateSeatPropertiesCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<UpdateSeatPropertiesCommandHandler> _logger;

    public UpdateSeatPropertiesCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<UpdateSeatPropertiesCommandHandler> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateSeatPropertiesCommand request, CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext)
            throw new InvalidOperationException("Database context is not compatible with transactions.");

        var seat = await _context.Seats
            .FirstOrDefaultAsync(s => s.Id == request.SeatId, cancellationToken);

        if (seat == null)
            throw new NotFoundException("Seat", request.SeatId);

        if (request.Status != SeatStatus.Inactive || seat.Status == SeatStatus.Inactive)
        {
            seat.Type = request.Type;
            seat.Status = request.Status;
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            seat.Type = request.Type;
            seat.Status = request.Status;

            var utcNow = DateTime.UtcNow;

            var activeFutureTickets = await _context.Tickets
                .Include(t => t.BookingSeat).ThenInclude(bs => bs.Seat)
                .Include(t => t.BookingSeat).ThenInclude(bs => bs.Booking).ThenInclude(b => b.Session)
                .Include(t => t.BookingSeat).ThenInclude(bs => bs.Booking).ThenInclude(b => b.Payment)
                .Where(t => t.BookingSeat.SeatId == request.SeatId &&
                            t.BookingSeat.Booking.Session.StartTime > utcNow &&
                            (t.BookingSeat.Booking.Status == BookingStatus.Confirmed ||
                             t.BookingSeat.Booking.Status == BookingStatus.Pending))
                .ToListAsync(cancellationToken);

            foreach (var ticket in activeFutureTickets)
            {
                var booking = ticket.BookingSeat.Booking;

                decimal priceMultiplier = ticket.BookingSeat.Seat.Type switch
                {
                    SeatType.VIP => 1.5m,
                    SeatType.Disabled => 0.8m,
                    _ => 1.0m
                };
                decimal ticketPrice = booking.Session.BasePrice * priceMultiplier;

                if (booking.Payment != null && !string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId))
                {
                    var refundResult = await _paymentService.RefundPaymentAsync(
                        booking.Payment.StripePaymentIntentId,
                        ticketPrice,
                        "uah",
                        cancellationToken
                    );

                    if (!refundResult)
                    {
                        _logger.LogWarning("Admin force-deactivation: Stripe refund failed for TicketId: {TicketId} inside BookingId: {BookingId}", ticket.Id, booking.Id);
                    }
                }

                _context.Tickets.Remove(ticket);
                _context.BookingSeats.Remove(ticket.BookingSeat);

                booking.TotalPrice -= ticketPrice;

                var hasOtherSeatsLeft = await _context.BookingSeats
                    .AnyAsync(bs => bs.BookingId == booking.Id && bs.Id != ticket.BookingSeatId, cancellationToken);

                if (!hasOtherSeatsLeft)
                {
                    booking.Status = BookingStatus.Cancelled;
                    if (booking.Payment != null)
                    {
                        booking.Payment.Status = PaymentStatus.Refunded;
                    }
                }
            }

            var activeSignalRLocks = await _context.SelectedSeats
                .Where(ss => ss.SeatId == request.SeatId)
                .ToListAsync(cancellationToken);

            if (activeSignalRLocks.Any())
            {
                _context.SelectedSeats.RemoveRange(activeSignalRLocks);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Unit.Value;
    }
}