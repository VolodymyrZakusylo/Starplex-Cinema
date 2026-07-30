using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Sessions.DTOs;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Sessions.Queries.GetSessionSeatMap;

public class GetSessionSeatMapQueryHandler : IRequestHandler<GetSessionSeatMapQuery, List<SeatMapDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSessionSeatMapQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SeatMapDto>> Handle(GetSessionSeatMapQuery request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var session = await _context.Sessions
            .AsNoTracking()
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session == null)
            throw new NotFoundException("Session", request.SessionId);

        if (session.Status == SessionStatus.Cancelled || session.Status == SessionStatus.Completed)
        {
            throw new InvalidOperationException("This session is no longer available for booking.");
        }

        if (session.Hall != null && !session.Hall.IsActive)
        {
            throw new InvalidOperationException("The cinema hall for this session is temporarily inactive.");
        }

        var allSeats = await _context.Seats
            .AsNoTracking()
            .Where(s => s.HallId == session.HallId)
            .ToListAsync(cancellationToken);

        var activeLocks = await _context.SelectedSeats
            .AsNoTracking()
            .Where(ss => ss.SessionId == request.SessionId && ss.LockedUntil > utcNow)
            .ToListAsync(cancellationToken);

        var takenSeatIds = await _context.BookingSeats
            .AsNoTracking()
            .Where(bs => bs.Booking.SessionId == request.SessionId &&
                         (bs.Booking.Status == BookingStatus.Confirmed ||
                          bs.Booking.Status == BookingStatus.Pending))
            .Select(bs => bs.SeatId)
            .ToListAsync(cancellationToken);

        var seatMap = allSeats.Select(seat =>
        {
            string status = "Available";
            Guid? lockedBy = null;

            if (seat.Status == SeatStatus.Inactive)
            {
                status = "Inactive";
            }
            else if (takenSeatIds.Contains(seat.Id))
            {
                status = "Taken";
            }
            else
            {
                var currentLock = activeLocks.FirstOrDefault(l => l.SeatId == seat.Id);
                if (currentLock != null)
                {
                    status = "Locked";
                    lockedBy = currentLock.UserId;
                }
            }

            decimal priceMultiplier = seat.Type switch
            {
                SeatType.VIP => 1.5m,
                SeatType.Disabled => 0.8m,
                _ => 1.0m
            };

            int.TryParse(seat.Row, out var parsedRow);

            return new SeatMapDto
            {
                SeatId = seat.Id,
                Row = seat.Row,
                SeatNumber = seat.Number,
                SeatType = seat.Type.ToString(),
                PriceMultiplier = priceMultiplier,
                Status = status,
                LockedByUserId = lockedBy,
                RowNumberForSorting = parsedRow
            };
        })
        .OrderBy(s => s.RowNumberForSorting)
        .ThenBy(s => s.SeatNumber)
        .ToList();

        return seatMap;
    }
}