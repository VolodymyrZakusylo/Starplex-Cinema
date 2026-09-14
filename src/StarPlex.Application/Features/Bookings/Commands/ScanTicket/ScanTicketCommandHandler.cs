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

        var utcNow = DateTime.UtcNow;
        var scanResult = ticket.Scan(utcNow, session);

        if (scanResult != ScanTicketResult.Success)
        {
            string errorMessage = scanResult switch
            {
                ScanTicketResult.InvalidStatus => $"Access Denied: Ticket status is '{booking.Status}'. It must be Confirmed.",
                ScanTicketResult.AlreadyScanned => "Already Scanned: This ticket has already been marked as USED!",
                ScanTicketResult.TooEarly => $"Too Early: Entrance is allowed 15 minutes before the show. Please wait {(int)Math.Ceiling((session.StartTime - utcNow).TotalMinutes)} more min.",
                ScanTicketResult.Expired => "Expired: This movie session has already ended.",
                _ => "Access Denied."
            };

            return new ScanTicketResultDto
            {
                IsSuccess = false,
                Message = errorMessage
            };
        }
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