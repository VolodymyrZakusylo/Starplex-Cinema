using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Bookings.Queries.GetUserBookings;

public class GetUserBookingsQuery : IRequest<List<UserBookingDto>>
{
    public Guid UserId { get; set; }
}

public class UserBookingDto
{
    public Guid Id { get; set; }
    public DateTime BookingDate { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string MovieImageUrl { get; set; } = string.Empty;
    public DateTime SessionStartTime { get; set; }
    public string HallName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public List<UserTicketDto> Tickets { get; set; } = new();
    public bool IsPast => SessionStartTime < DateTime.UtcNow;
}

public class UserTicketDto
{
    public Guid TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public int Row { get; set; }
    public int SeatNumber { get; set; }
    public SeatType SeatType { get; set; }
}

public class GetUserBookingsQueryHandler : IRequestHandler<GetUserBookingsQuery, List<UserBookingDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserBookingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserBookingDto>> Handle(GetUserBookingsQuery request, CancellationToken cancellationToken)
    {
        var bookings = await _context.Bookings
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Ticket)
            .Include(b => b.Session).ThenInclude(s => s.Movie)
            .Include(b => b.Session).ThenInclude(s => s.Hall)
            .Where(b => b.UserId == request.UserId)
            .OrderByDescending(b => b.BookingTime)
            .ToListAsync(cancellationToken);

        return bookings.Select(b =>
        {
            var movie = b.Session.Movie;
            var session = b.Session;

            return new UserBookingDto
            {
                Id = b.Id,
                BookingDate = b.BookingTime,
                MovieTitle = movie.Title,
                MovieImageUrl = movie.PosterUrl,
                SessionStartTime = session.StartTime,
                HallName = session.Hall.Name,
                TotalPrice = b.TotalPrice,
                Tickets = b.BookingSeats
                    .Where(bs => bs.Ticket != null)
                    .Select(bs => new UserTicketDto
                    {
                        TicketId = bs.Ticket.Id,
                        TicketCode = bs.Ticket.TicketCode,
                        Row = int.TryParse(bs.Seat.Row, out var r) ? r : 0,
                        SeatNumber = bs.Seat.Number,
                        SeatType = bs.Seat.Type
                    }).ToList()
            };
        }).ToList();
    }
}