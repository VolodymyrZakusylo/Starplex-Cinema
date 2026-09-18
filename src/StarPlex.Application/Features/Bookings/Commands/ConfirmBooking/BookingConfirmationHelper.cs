using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Domain.Factories;

namespace StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;

public static class BookingConfirmationHelper
{
    public static async Task<(bool IsSuccess, bool WasNewlyConfirmed)> ExecuteAtomicConfirmationAsync(
        IApplicationDbContext context,
        Guid bookingId,
        string expectedStripePaymentIntentId,
        decimal expectedAmount,
        string expectedCurrency,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (context is not DbContext dbContext) return (false, false);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (dbContext.Database.ProviderName?.Contains("Npgsql") == true)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Bookings\" WHERE \"Id\" = {bookingId} FOR UPDATE", cancellationToken);
        }

        var booking = await context.Bookings
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null) return (false, false);

        if (booking.Payment == null || string.IsNullOrWhiteSpace(booking.Payment.StripePaymentIntentId))
        {
            logger.LogWarning("Payment entity or StripePaymentIntentId is missing for booking {BookingId}", bookingId);
            return (false, false);
        }

        if (!string.Equals(booking.Payment.StripePaymentIntentId, expectedStripePaymentIntentId, StringComparison.Ordinal))
        {
            logger.LogWarning("PaymentIntentId mismatch for booking {BookingId}: stored={Stored}, expected={Expected}",
                bookingId, booking.Payment.StripePaymentIntentId, expectedStripePaymentIntentId);
            return (false, false);
        }

        // Verify consistency between persisted Payment.Amount and Booking.TotalPrice
        if (booking.Payment.Amount != booking.TotalPrice)
        {
            logger.LogWarning("Inconsistent persisted Payment.Amount ({PaymentAmount}) vs Booking.TotalPrice ({TotalPrice}) for booking {BookingId}",
                booking.Payment.Amount, booking.TotalPrice, bookingId);
            return (false, false);
        }

        // Verify expected amount matches persisted Payment.Amount
        if (booking.Payment.Amount != expectedAmount)
        {
            logger.LogWarning("Amount mismatch for booking {BookingId}: payment={PaymentAmount}, expected={Expected}",
                bookingId, booking.Payment.Amount, expectedAmount);
            return (false, false);
        }

        if (!string.Equals(expectedCurrency, "uah", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Currency mismatch for booking {BookingId}: expected={Expected}", bookingId, expectedCurrency);
            return (false, false);
        }

        if (booking.Status == BookingStatus.Confirmed)
        {
            await transaction.CommitAsync(cancellationToken);
            return (true, false);
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return (false, false);
        }

        if (booking.Status != BookingStatus.Pending)
        {
            return (false, false);
        }

        booking.Status = BookingStatus.Confirmed;
        var utcNow = DateTime.UtcNow;

        booking.Payment.Status = PaymentStatus.Succeeded;
        booking.Payment.PaidAt = utcNow;

        var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
        var temporaryLocks = await context.SelectedSeats
            .Where(ss => ss.SessionId == booking.SessionId && seatIds.Contains(ss.SeatId))
            .ToListAsync(cancellationToken);

        if (temporaryLocks.Any())
        {
            context.SelectedSeats.RemoveRange(temporaryLocks);
        }

        foreach (var bookingSeat in booking.BookingSeats)
        {
            var ticket = TicketFactory.CreateForSeat(bookingSeat.Id);
            context.Tickets.Add(ticket);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (true, true);
    }

    public static async Task SendTicketNotificationsAsync(
        IApplicationDbContext context,
        ITicketService ticketService,
        IEmailService emailService,
        IUserService userService,
        Guid bookingId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var booking = await context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Session).ThenInclude(s => s.Movie)
                .Include(b => b.Session).ThenInclude(s => s.Hall)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null) return;

            var bookingSeatIds = booking.BookingSeats.Select(bs => bs.Id).ToList();
            var tickets = await context.Tickets
                .Where(t => bookingSeatIds.Contains(t.BookingSeatId))
                .ToListAsync(cancellationToken);

            var userInfo = await userService.GetUserContactInfoAsync(booking.UserId, cancellationToken);

            if (userInfo != null && !string.IsNullOrEmpty(userInfo.Value.Email))
            {
                var ticketCodes = new List<string>();
                var ticketPdfs = new List<byte[]>();

                foreach (var ticket in tickets)
                {
                    var pdfBytes = await ticketService.GenerateTicketPdfAsync(ticket.Id, cancellationToken);
                    ticketCodes.Add(ticket.TicketCode);
                    ticketPdfs.Add(pdfBytes);
                }

                await emailService.SendTicketEmailAsync(
                    toEmail: userInfo.Value.Email,
                    recipientName: userInfo.Value.FullName,
                    movieTitle: booking.Session.Movie.Title,
                    sessionStartTime: booking.Session.StartTime,
                    hallName: booking.Session.Hall.Name,
                    ticketCodes: ticketCodes,
                    ticketPdfs: ticketPdfs,
                    cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send ticket email for booking {BookingId}", bookingId);
        }
    }
}
