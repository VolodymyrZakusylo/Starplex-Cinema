using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using StarPlex.Domain.Factories;

namespace StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;

public class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITicketService _ticketService;
    private readonly IEmailService _emailService;
    private readonly IUserService _userService;
    private readonly ILogger<ConfirmBookingCommandHandler> _logger;

    public ConfirmBookingCommandHandler(
        IApplicationDbContext context,
        ITicketService ticketService,
        IEmailService emailService,
        IUserService userService,
        ILogger<ConfirmBookingCommandHandler> logger)
    {
        _context = context;
        _ticketService = ticketService;
        _emailService = emailService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<bool> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext) return false;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var booking = await _context.Bookings
                .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
                .Include(b => b.Session).ThenInclude(s => s.Movie)
                .Include(b => b.Session).ThenInclude(s => s.Hall)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null) return false;
            if (booking.Status == BookingStatus.Confirmed) return true;

            booking.Status = BookingStatus.Confirmed;
            var utcNow = DateTime.UtcNow;

            var seatIds = booking.BookingSeats.Select(bs => bs.SeatId).ToList();
            var temporaryLocks = await _context.SelectedSeats
                .Where(ss => ss.SessionId == booking.SessionId && seatIds.Contains(ss.SeatId))
                .ToListAsync(cancellationToken);

            if (temporaryLocks.Any())
            {
                _context.SelectedSeats.RemoveRange(temporaryLocks);
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == booking.Id, cancellationToken);

            if (payment != null)
            {
                payment.Status = PaymentStatus.Succeeded;
                payment.PaidAt = utcNow;
            }

            var createdTickets = new List<Ticket>();

            foreach (var bookingSeat in booking.BookingSeats)
            {
                var ticket = TicketFactory.CreateForSeat(bookingSeat.Id);

                _context.Tickets.Add(ticket);
                createdTickets.Add(ticket);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            try
            {
                var userInfo = await _userService.GetUserContactInfoAsync(booking.UserId, cancellationToken);

                if (userInfo != null && !string.IsNullOrEmpty(userInfo.Value.Email))
                {
                    var ticketCodes = new List<string>();
                    var ticketPdfs = new List<byte[]>();

                    foreach (var ticket in createdTickets)
                    {
                        var pdfBytes = await _ticketService.GenerateTicketPdfAsync(ticket.Id, cancellationToken);

                        ticketCodes.Add(ticket.TicketCode);
                        ticketPdfs.Add(pdfBytes);
                    }

                    await _emailService.SendTicketEmailAsync(
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
                _logger.LogError(ex, "Failed to send ticket email for booking {BookingId}", booking.Id);
            }

            return true;
    }
}