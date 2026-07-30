using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Bookings.Commands.CancelTicket;

public class CancelTicketCommandHandler : IRequestHandler<CancelTicketCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<CancelTicketCommandHandler> _logger;

    public CancelTicketCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<CancelTicketCommandHandler> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<bool> Handle(CancelTicketCommand request, CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext) return false;

        var ticket = await _context.Tickets
            .Include(t => t.BookingSeat).ThenInclude(bs => bs.Seat)
            .Include(t => t.BookingSeat).ThenInclude(bs => bs.Booking).ThenInclude(b => b.Session)
            .Include(t => t.BookingSeat).ThenInclude(bs => bs.Booking).ThenInclude(b => b.Payment)
            .Include(t => t.BookingSeat).ThenInclude(bs => bs.Booking).ThenInclude(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null) return false;

        var booking = ticket.BookingSeat.Booking;

        if (booking.UserId != request.UserId) return false;
        if (DateTime.UtcNow >= booking.Session.StartTime.AddMinutes(-60)) return false;
        if (booking.Payment == null || string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId)) return false;

        decimal priceMultiplier = ticket.BookingSeat.Seat.Type switch
        {
            SeatType.VIP => 1.5m,
            SeatType.Disabled => 0.8m,
            _ => 1.0m
        };
        decimal baseTicketPrice = booking.Session.BasePrice * priceMultiplier;

        decimal refundAmount = baseTicketPrice;
        if (booking.DiscountId != null)
        {
            decimal totalBasePrice = booking.BookingSeats.Sum(bs => bs.Seat.Type switch
            {
                SeatType.VIP => booking.Session.BasePrice * 1.5m,
                SeatType.Disabled => booking.Session.BasePrice * 0.8m,
                _ => booking.Session.BasePrice
            });

            if (totalBasePrice > 0)
            {
                decimal discountRatio = booking.TotalPrice / totalBasePrice;
                refundAmount = Math.Round(baseTicketPrice * discountRatio, 2);
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var refundResult = await _paymentService.RefundPaymentAsync(
                booking.Payment.StripePaymentIntentId,
                refundAmount,
                "uah",
                cancellationToken
            );

            if (!refundResult)
            {
                _logger.LogWarning("Stripe refund failed via IPaymentService for TicketId: {TicketId}", request.TicketId);
                return false;
            }

            _context.Tickets.Remove(ticket);
            _context.BookingSeats.Remove(ticket.BookingSeat);

            var hasOtherSeats = await _context.BookingSeats
                .AnyAsync(bs => bs.BookingId == booking.Id && bs.Id != ticket.BookingSeatId, cancellationToken);

            if (!hasOtherSeats)
            {
                booking.Status = BookingStatus.Cancelled;
                if (booking.Payment != null)
                {
                    booking.Payment.Status = PaymentStatus.Refunded;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to execute CancelTicketCommand for TicketId: {TicketId}", request.TicketId);
            return false;
        }
    }
}