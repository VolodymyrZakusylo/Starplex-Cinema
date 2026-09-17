using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;

public class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITicketService _ticketService;
    private readonly IEmailService _emailService;
    private readonly IUserService _userService;
    private readonly ILogger<ConfirmBookingCommandHandler> _logger;

    public ConfirmBookingCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ICurrentUserService currentUserService,
        ITicketService ticketService,
        IEmailService emailService,
        IUserService userService,
        ILogger<ConfirmBookingCommandHandler> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _currentUserService = currentUserService;
        _ticketService = ticketService;
        _emailService = emailService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<bool> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
    {
        // 1. Mandatory ownership check evaluated FIRST
        var bookingInfo = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == request.BookingId)
            .Select(b => new { b.Id, b.UserId, b.TotalPrice, b.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (bookingInfo == null) return false;

        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null || currentUserId.Value != bookingInfo.UserId)
        {
            return false;
        }

        // 2. Owner-safe idempotent status handling (without Stripe network call)
        if (bookingInfo.Status == BookingStatus.Confirmed)
        {
            return true;
        }

        if (bookingInfo.Status != BookingStatus.Pending)
        {
            return false;
        }

        // 3. Fetch payment info for server-side Stripe verification
        var paymentInfo = await _context.Payments
            .AsNoTracking()
            .Where(p => p.BookingId == request.BookingId)
            .Select(p => new { p.StripePaymentIntentId, p.Amount })
            .FirstOrDefaultAsync(cancellationToken);

        if (paymentInfo == null || string.IsNullOrEmpty(paymentInfo.StripePaymentIntentId))
        {
            return false;
        }

        // 4. Validate consistency between Payment.Amount and Booking.TotalPrice
        if (paymentInfo.Amount != bookingInfo.TotalPrice)
        {
            _logger.LogWarning("Payment.Amount ({PaymentAmount}) does not match Booking.TotalPrice ({TotalPrice}) for booking {BookingId}",
                paymentInfo.Amount, bookingInfo.TotalPrice, request.BookingId);
            return false;
        }

        // 5. Perform server-side Stripe verification outside database transaction boundary
        bool isStripeValid = await _paymentService.VerifyPaymentIntentAsync(
            paymentInfo.StripePaymentIntentId,
            bookingInfo.Id,
            paymentInfo.Amount,
            "uah",
            cancellationToken);

        if (!isStripeValid)
        {
            return false;
        }

        // 6. Perform atomic confirmation transaction under FOR UPDATE PostgreSQL row lock
        var (isSuccess, wasNewlyConfirmed) = await BookingConfirmationHelper.ExecuteAtomicConfirmationAsync(
            _context,
            request.BookingId,
            expectedStripePaymentIntentId: paymentInfo.StripePaymentIntentId,
            expectedAmount: paymentInfo.Amount,
            expectedCurrency: "uah",
            _logger,
            cancellationToken);

        if (!isSuccess) return false;

        // 7. Trigger ticket PDF and email notification side-effects ONLY IF state actually transitioned
        if (wasNewlyConfirmed)
        {
            await BookingConfirmationHelper.SendTicketNotificationsAsync(
                _context,
                _ticketService,
                _emailService,
                _userService,
                request.BookingId,
                _logger,
                cancellationToken);
        }

        return true;
    }
}