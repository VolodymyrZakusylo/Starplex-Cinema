using StarPlex.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Helpers;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Sessions.Commands.MoveSession;

public class MoveSessionCommandHandler : IRequestHandler<MoveSessionCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private const int CleanUpDurationInMinutes = 20;

    public MoveSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(MoveSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .Include(s => s.Movie)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session == null)
            throw new NotFoundException("Session", request.SessionId);

        if (session.OriginalPrice == 0 && session.BasePrice > 0)
        {
            session.OriginalPrice = session.BasePrice;
        }

        var rawTime = request.NewStartTime;
        int rem = rawTime.Minute % 5;
        var adjustedStartTime = rem == 0 ? rawTime : rawTime.AddMinutes(5 - rem).AddSeconds(-rawTime.Second).AddMilliseconds(-rawTime.Millisecond);
        adjustedStartTime = DateTime.SpecifyKind(adjustedStartTime, DateTimeKind.Utc);

        var hasBookings = await _context.Bookings
            .AnyAsync(b => b.SessionId == request.SessionId && b.Status == BookingStatus.Confirmed, cancellationToken);

        if (hasBookings && (session.StartTime != adjustedStartTime || session.HallId != request.HallId))
        {
            throw new BusinessRuleException("Cannot move session because there are active bookings for this session.");
        }

        var adjustedEndTime = adjustedStartTime.AddMinutes(session.MovieDurationInMinutes + CleanUpDurationInMinutes);

        var kyivTzi = TimeZoneHelpers.KyivTimeZone;
        var startTimeKyiv = TimeZoneInfo.ConvertTimeFromUtc(adjustedStartTime, kyivTzi);
        var occupiedEndTimeKyiv = TimeZoneInfo.ConvertTimeFromUtc(adjustedEndTime, kyivTzi);

        var startOfWorkingDayKyiv = new DateTime(startTimeKyiv.Year, startTimeKyiv.Month, startTimeKyiv.Day, 10, 0, 0);
        var endOfWorkingDayKyiv = new DateTime(startTimeKyiv.Year, startTimeKyiv.Month, startTimeKyiv.Day, 23, 0, 0);

        if (startTimeKyiv.Date != occupiedEndTimeKyiv.Date ||
            startTimeKyiv < startOfWorkingDayKyiv ||
            occupiedEndTimeKyiv > endOfWorkingDayKyiv)
        {
            throw new BusinessRuleException("The rescheduled session falls outside the cinema's working hours (10:00 - 23:00 Kyiv time).");
        }

        var hasCollision = await _context.Sessions
            .AnyAsync(s => s.Id != session.Id &&
                           s.HallId == request.HallId &&
                           s.Status == SessionStatus.Active &&
                           adjustedStartTime < s.StartTime.AddMinutes(s.MovieDurationInMinutes + CleanUpDurationInMinutes) &&
                           adjustedEndTime > s.StartTime,
                      cancellationToken);

        if (hasCollision)
            throw new ConflictException("Selected time slot is already occupied by another session or its cleaning interval. Choose an empty space.");

        session.HallId = request.HallId;
        session.StartTime = adjustedStartTime;

        session.BasePrice = startTimeKyiv.Hour switch
        {
            >= 10 and < 12 => Math.Round(session.OriginalPrice * 0.80m, 0),
            >= 12 and < 17 => session.OriginalPrice,
            >= 17 and < 21 => Math.Round(session.OriginalPrice * 1.25m, 0),
            _ => Math.Round(session.OriginalPrice * 1.10m, 0)
        };

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}