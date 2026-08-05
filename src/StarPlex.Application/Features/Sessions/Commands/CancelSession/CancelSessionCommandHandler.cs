using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Sessions.Commands.CancelSession;

public class CancelSessionCommandHandler : IRequestHandler<CancelSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<CancelSessionCommandHandler> _logger;

    public CancelSessionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPaymentService paymentService,
        ILogger<CancelSessionCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task Handle(CancelSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (session == null) throw new NotFoundException("Session", request.Id);

        if (!_currentUserService.IsSuperAdmin && _currentUserService.CinemaId != session.Hall.CinemaId)
        {
            throw new ForbiddenException("You do not have permission to manage this cinema.");
        }

        session.Status = SessionStatus.Cancelled;

        var activeBookings = await _context.Bookings
            .Include(b => b.Payment)
            .Include(b => b.BookingSeats)
            .Where(b => b.SessionId == session.Id && b.Status == BookingStatus.Confirmed)
            .ToListAsync(cancellationToken);

        foreach (var booking in activeBookings)
        {
            if (booking.Payment != null && !string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId) && booking.Payment.Status == PaymentStatus.Succeeded)
            {
                try
                {
                    var refundResult = await _paymentService.RefundPaymentAsync(
                        booking.Payment.StripePaymentIntentId,
                        booking.TotalPrice,
                        "uah",
                        cancellationToken
                    );

                    if (refundResult)
                    {
                        booking.Status = BookingStatus.Cancelled;
                        booking.Payment.Status = PaymentStatus.Refunded;

                        var seatIds = booking.BookingSeats.Select(bs => bs.Id).ToList();
                        var ticketsToRemove = await _context.Tickets
                            .Where(t => seatIds.Contains(t.BookingSeatId))
                            .ToListAsync(cancellationToken);

                        if (ticketsToRemove.Any())
                        {
                            _context.Tickets.RemoveRange(ticketsToRemove);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Automatic Stripe refund failed for BookingId: {BookingId} during cancellation of SessionId: {SessionId}", booking.Id, session.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute refund for BookingId: {BookingId} during cancellation of SessionId: {SessionId}", booking.Id, session.Id);
                }
            }
            else
            {
                booking.Status = BookingStatus.Cancelled;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}