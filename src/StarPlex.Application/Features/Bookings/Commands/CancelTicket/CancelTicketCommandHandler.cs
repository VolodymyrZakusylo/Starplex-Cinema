using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Exceptions;
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
            .Include(t => t.BookingSeat)
            .Include(t => t.BookingSeat.Booking).ThenInclude(b => b.Session)
            .Include(t => t.BookingSeat.Booking).ThenInclude(b => b.Payment)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null) return false;

        var booking = ticket.BookingSeat.Booking;

        if (booking.UserId != request.UserId) return false;
        if (!booking.CanCancel(DateTime.UtcNow)) return false;
        if (booking.Payment == null || string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId)) return false;

        decimal refundAmount = ticket.BookingSeat.PurchasePrice;
        if (refundAmount <= 0)
        {
            throw new BusinessRuleException("Historical purchase price is not available for this ticket.");
        }

        string idempotencyKey = $"ticket_refund_{request.TicketId}";

        var refundResult = await _paymentService.RefundPaymentAsync(
            booking.Payment.StripePaymentIntentId,
            refundAmount,
            "uah",
            cancellationToken,
            idempotencyKey: idempotencyKey
        );

        if (!refundResult)
        {
            _logger.LogWarning("Stripe refund failed via IPaymentService for TicketId: {TicketId}", request.TicketId);
            return false;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (dbContext.Database.ProviderName?.Contains("Npgsql") == true)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"Bookings\" WHERE \"Id\" = {booking.Id} FOR UPDATE", cancellationToken);
        }

        var ticketToCancel = await _context.Tickets
            .Include(t => t.BookingSeat)
            .Include(t => t.BookingSeat.Booking).ThenInclude(b => b.Payment)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticketToCancel == null)
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        var bookingToUpdate = ticketToCancel.BookingSeat.Booking;

        _context.Tickets.Remove(ticketToCancel);
        _context.BookingSeats.Remove(ticketToCancel.BookingSeat);

        var remainingSeatsCount = await _context.BookingSeats
            .CountAsync(bs => bs.BookingId == bookingToUpdate.Id && bs.Id != ticketToCancel.BookingSeatId, cancellationToken);

        bookingToUpdate.CancelIfEmpty(remainingSeatsCount);
        if (remainingSeatsCount == 0 && bookingToUpdate.Payment != null)
        {
            bookingToUpdate.Payment.Status = PaymentStatus.Refunded;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}