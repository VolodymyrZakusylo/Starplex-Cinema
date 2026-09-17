using StarPlex.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Helpers;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Sessions.Commands.GenerateSchedule;

public class GenerateScheduleCommandHandler : IRequestHandler<GenerateScheduleCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private const int CleanUpDurationInMinutes = 20;

    public GenerateScheduleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(GenerateScheduleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsSuperAdmin &&
            (!_currentUserService.CinemaId.HasValue || _currentUserService.CinemaId != request.CinemaId))
        {
            throw new ForbiddenException("You do not have permission to manage this cinema.");
        }

        var kyivTzi = TimeZoneHelpers.KyivTimeZone;
        var nowKyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kyivTzi);
        var todayKyivDate = nowKyiv.Date;
        var requestedKyivDate = new DateTime(request.TargetDate.Year, request.TargetDate.Month, request.TargetDate.Day);

        if (requestedKyivDate < todayKyivDate)
            throw new BusinessRuleException("Cannot generate a schedule for a past date.");

        var halls = await _context.Halls
            .Where(h => h.CinemaId == request.CinemaId && h.IsActive)
            .OrderBy(h => h.Name)
            .ThenBy(h => h.Id)
            .ToListAsync(cancellationToken);

        if (!halls.Any())
            throw new BusinessRuleException("There are no active halls in this cinema to generate a schedule.");

        var startOfKyivCalendarDay = new DateTime(request.TargetDate.Year, request.TargetDate.Month, request.TargetDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var endOfKyivCalendarDay = startOfKyivCalendarDay.AddDays(1);

        var startOfCalendarDayUtc = TimeZoneInfo.ConvertTimeToUtc(startOfKyivCalendarDay, kyivTzi);
        var endOfCalendarDayUtc = TimeZoneInfo.ConvertTimeToUtc(endOfKyivCalendarDay, kyivTzi);

        var existingSessionsOnDate = await _context.Sessions
            .Where(s => s.Status == SessionStatus.Active &&
                        s.StartTime >= startOfCalendarDayUtc &&
                        s.StartTime < endOfCalendarDayUtc &&
                        halls.Select(h => h.Id).Contains(s.HallId))
            .ToListAsync(cancellationToken);

        if (existingSessionsOnDate.Any())
            throw new ConflictException($"Schedule for {request.TargetDate:dd.MM.yyyy} already exists ({existingSessionsOnDate.Count} sessions found). Please clear it first.");

        var startOfKyivDay = new DateTime(request.TargetDate.Year, request.TargetDate.Month, request.TargetDate.Day, 10, 0, 0, DateTimeKind.Unspecified);
        var endOfKyivDay = new DateTime(request.TargetDate.Year, request.TargetDate.Month, request.TargetDate.Day, 23, 0, 0, DateTimeKind.Unspecified);

        var movies = await _context.Movies
            .Where(m => request.MovieIds.Contains(m.Id) && m.Status == MovieStatus.NowShowing)
            .OrderByDescending(m => m.TmdbRating)
            .ToListAsync(cancellationToken);

        if (!movies.Any())
            throw new BusinessRuleException("No active movies were found for generation.");

        int sessionsCreated = 0;

        for (int hallIndex = 0; hallIndex < halls.Count; hallIndex++)
        {
            var hall = halls[hallIndex];
            int movieIndex = hallIndex;

            var currentKyivTrackTime = startOfKyivDay;

            while (currentKyivTrackTime < endOfKyivDay)
            {
                var currentMovie = movies[movieIndex % movies.Count];

                var sessionStartTimeKyiv = currentKyivTrackTime;
                var sessionEndTimeKyiv = sessionStartTimeKyiv.AddMinutes(currentMovie.DurationInMinutes);

                if (sessionEndTimeKyiv > endOfKyivDay)
                {
                    break;
                }

                var sessionStartTimeUtc = TimeZoneInfo.ConvertTimeToUtc(sessionStartTimeKyiv, kyivTzi);
                var sessionEndTimeUtc = TimeZoneInfo.ConvertTimeToUtc(sessionEndTimeKyiv, kyivTzi);

                bool hasCollision = await _context.Sessions.AnyAsync(s =>
                    s.HallId == hall.Id &&
                    s.Status == SessionStatus.Active &&
                    ((sessionStartTimeUtc >= s.StartTime && sessionStartTimeUtc < s.StartTime.AddMinutes(s.MovieDurationInMinutes + CleanUpDurationInMinutes)) ||
                     (sessionEndTimeUtc > s.StartTime && sessionEndTimeUtc <= s.StartTime.AddMinutes(s.MovieDurationInMinutes + CleanUpDurationInMinutes))),
                    cancellationToken);

                if (!hasCollision)
                {
                    decimal calculatedPrice = sessionStartTimeKyiv.Hour switch
                    {
                        >= 10 and < 12 => request.BasePrice * 0.80m,
                        >= 12 and < 17 => request.BasePrice,
                        >= 17 and < 21 => request.BasePrice * 1.25m,
                        _ => request.BasePrice * 1.10m
                    };

                    var newSession = new Session
                    {
                        Id = Guid.NewGuid(),
                        MovieId = currentMovie.Id,
                        HallId = hall.Id,
                        StartTime = sessionStartTimeUtc,
                        MovieDurationInMinutes = currentMovie.DurationInMinutes,
                        OriginalPrice = request.BasePrice,
                        BasePrice = Math.Round(calculatedPrice, 0),
                        Status = SessionStatus.Active
                    };

                    _context.Sessions.Add(newSession);
                    sessionsCreated++;
                }

                var nextTimeWithCleanUpKyiv = sessionEndTimeKyiv.AddMinutes(CleanUpDurationInMinutes);
                int minutesToSubtractOrAdd = nextTimeWithCleanUpKyiv.Minute % 5;

                currentKyivTrackTime = minutesToSubtractOrAdd == 0
                    ? nextTimeWithCleanUpKyiv
                    : nextTimeWithCleanUpKyiv.AddMinutes(5 - minutesToSubtractOrAdd);

                movieIndex++;
            }
        }

        if (sessionsCreated > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return sessionsCreated;
    }
}
