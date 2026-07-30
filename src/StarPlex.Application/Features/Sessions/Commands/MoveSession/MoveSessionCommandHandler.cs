using MediatR;
using Microsoft.EntityFrameworkCore;
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
            throw new InvalidOperationException("Session not found.");

        if (session.OriginalPrice == 0 && session.BasePrice > 0)
        {
            session.OriginalPrice = session.BasePrice;
        }

        var rawTime = request.NewStartTime;
        int rem = rawTime.Minute % 5;
        var adjustedStartTime = rem == 0 ? rawTime : rawTime.AddMinutes(5 - rem).AddSeconds(-rawTime.Second).AddMilliseconds(-rawTime.Millisecond);
        adjustedStartTime = DateTime.SpecifyKind(adjustedStartTime, DateTimeKind.Utc);

        var adjustedEndTime = adjustedStartTime.AddMinutes(session.MovieDurationInMinutes + CleanUpDurationInMinutes);

        var startOfWorkingDay = new DateTime(adjustedStartTime.Year, adjustedStartTime.Month, adjustedStartTime.Day, 10, 0, 0, DateTimeKind.Utc);
        var endOfWorkingDay = new DateTime(adjustedStartTime.Year, adjustedStartTime.Month, adjustedStartTime.Day, 23, 0, 0, DateTimeKind.Utc);

        if (adjustedStartTime < startOfWorkingDay || adjustedEndTime > endOfWorkingDay)
            throw new InvalidOperationException("The rescheduled session falls outside the cinema's working hours (10:00 - 23:00).");

        var hasCollision = await _context.Sessions
            .AnyAsync(s => s.Id != session.Id &&
                           s.HallId == request.HallId &&
                           s.Status == SessionStatus.Active &&
                           adjustedStartTime < s.StartTime.AddMinutes(s.MovieDurationInMinutes + CleanUpDurationInMinutes) &&
                           adjustedEndTime > s.StartTime,
                      cancellationToken);

        if (hasCollision)
            throw new InvalidOperationException("Selected time slot is already occupied by another session or its cleaning interval. Choose an empty space.");

        session.HallId = request.HallId;
        session.StartTime = adjustedStartTime;

        session.BasePrice = adjustedStartTime.Hour switch
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