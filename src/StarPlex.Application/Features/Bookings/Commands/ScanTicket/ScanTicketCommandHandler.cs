using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StarPlex.Application.Features.Bookings.Commands.ScanTicket;

public class ScanTicketCommandHandler : IRequestHandler<ScanTicketCommand, ScanTicketResultDto>
{
    private readonly IApplicationDbContext _context;

    public ScanTicketCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ScanTicketResultDto> Handle(ScanTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Seat)
            .Include(t => t.BookingSeat)
                .ThenInclude(bs => bs.Booking)
                    .ThenInclude(b => b.Session)
                        .ThenInclude(s => s.Movie)
            .Include(t => t.BookingSeat.Booking.Session.Hall)
            .FirstOrDefaultAsync(t => t.TicketCode == request.TicketCode.Trim(), cancellationToken);

        if (ticket == null)
        {
            return new ScanTicketResultDto { IsSuccess = false, Message = "Ticket not found in the StarPlex database." };
        }

        var booking = ticket.BookingSeat.Booking;
        var session = booking.Session;

        if (booking.Status != BookingStatus.Confirmed)
        {
            return new ScanTicketResultDto
            {
                IsSuccess = false,
                Message = $"Access Denied: Ticket status is '{booking.Status}'. It must be Confirmed."
            };
        }

        if (ticket.IsUsed)
        {
            return new ScanTicketResultDto
            {
                IsSuccess = false,
                Message = "Already Scanned: This ticket has already been marked as USED!"
            };
        }

        var utcNow = DateTime.UtcNow;
        var allowedEntryStart = session.StartTime.AddMinutes(-15);
        var sessionEndTime = session.StartTime.AddMinutes(session.MovieDurationInMinutes);

        if (utcNow < allowedEntryStart)
        {
            var minutesToWait = (int)Math.Ceiling((session.StartTime - utcNow).TotalMinutes);
            return new ScanTicketResultDto
            {
                IsSuccess = false,
                Message = $"Too Early: Entrance is allowed 15 minutes before the show. Please wait {minutesToWait} more min."
            };
        }

        if (utcNow > sessionEndTime)
        {
            return new ScanTicketResultDto
            {
                IsSuccess = false,
                Message = "Expired: This movie session has already ended."
            };
        }

        ticket.IsUsed = true;
        await _context.SaveChangesAsync(cancellationToken);

        return new ScanTicketResultDto
        {
            IsSuccess = true,
            Message = "Access Granted! Ticket successfully checked in.",
            MovieTitle = session.Movie.Title,
            HallName = session.Hall.Name,
            Row = ticket.BookingSeat.Seat.Row,
            Number = ticket.BookingSeat.Seat.Number,
            StartTime = session.StartTime.ToString("HH:mm")
        };
    }
}