using MediatR;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;

namespace StarPlex.Application.Features.Bookings.Commands.ConfirmBookingFromWebhook;

public class ConfirmBookingFromWebhookCommandHandler : IRequestHandler<ConfirmBookingFromWebhookCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITicketService _ticketService;
    private readonly IEmailService _emailService;
    private readonly IUserService _userService;
    private readonly ILogger<ConfirmBookingFromWebhookCommandHandler> _logger;

    public ConfirmBookingFromWebhookCommandHandler(
        IApplicationDbContext context,
        ITicketService ticketService,
        IEmailService emailService,
        IUserService userService,
        ILogger<ConfirmBookingFromWebhookCommandHandler> logger)
    {
        _context = context;
        _ticketService = ticketService;
        _emailService = emailService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<bool> Handle(ConfirmBookingFromWebhookCommand request, CancellationToken cancellationToken)
    {
        // 1. Perform atomic confirmation transaction under FOR UPDATE PostgreSQL row lock
        // Note: Stripe HTTP retrieve is NOT performed on webhook path as event is already signature-verified
        var (isSuccess, wasNewlyConfirmed) = await BookingConfirmationHelper.ExecuteAtomicConfirmationAsync(
            _context,
            request.BookingId,
            expectedStripePaymentIntentId: request.PaymentIntentId,
            expectedAmount: request.Amount,
            expectedCurrency: request.Currency,
            _logger,
            cancellationToken);

        if (!isSuccess) return false;

        // 2. Trigger ticket PDF and email notification side-effects ONLY IF state actually transitioned
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
