using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Sessions.Commands.GenerateSchedule;

public class GenerateScheduleCommandHandler : IRequestHandler<GenerateScheduleCommand, int>
{
    private readonly IApplicationDbContext _context;
    private const int CleanUpDurationInMinutes = 20;

    public GenerateScheduleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(GenerateScheduleCommand request, CancellationToken cancellationToken)
    {
        var halls = await _context.Halls
            .Where(h => h.CinemaId == request.CinemaId && h.IsActive)
            .ToListAsync(cancellationToken);

        if (!halls.Any())
            throw new InvalidOperationException("There are no active halls in this cinema to generate a schedule.");

        var existingSessionsOnDate = await _context.Sessions
            .Where(s => s.Status == SessionStatus.Active &&
                        s.StartTime.Date == request.TargetDate.Date &&
                        halls.Select(h => h.Id).Contains(s.HallId))
            .ToListAsync(cancellationToken);

        if (existingSessionsOnDate.Any())
            throw new InvalidOperationException($"Schedule for {request.TargetDate:dd.MM.yyyy} already exists ({existingSessionsOnDate.Count} sessions found). Please clear it first.");

        var movies = await _context.Movies
            .Where(m => request.MovieIds.Contains(m.Id) && m.Status == MovieStatus.NowShowing)
            .OrderByDescending(m => m.TmdbRating)
            .ToListAsync(cancellationToken);

        if (!movies.Any())
            throw new InvalidOperationException("No active movies were found for generation.");

        int sessionsCreated = 0;

        foreach (var hall in halls)
        {
            int movieIndex = 0;

            var startOfDay = new DateTime(request.TargetDate.Year, request.TargetDate.Month, request.TargetDate.Day, 10, 0, 0, DateTimeKind.Utc);
            var endOfDay = new DateTime(request.TargetDate.Year, request.TargetDate.Month, request.TargetDate.Day, 23, 0, 0, DateTimeKind.Utc);

            var currentTrackTime = startOfDay;

            while (currentTrackTime < endOfDay)
            {
                var currentMovie = movies[movieIndex % movies.Count];

                var sessionStartTime = currentTrackTime;
                var sessionEndTime = sessionStartTime.AddMinutes(currentMovie.DurationInMinutes);

                if (sessionEndTime > endOfDay)
                {
                    break;
                }

                bool hasCollision = await _context.Sessions.AnyAsync(s =>
                    s.HallId == hall.Id &&
                    s.Status == SessionStatus.Active &&
                    ((sessionStartTime >= s.StartTime && sessionStartTime < s.StartTime.AddMinutes(s.MovieDurationInMinutes + CleanUpDurationInMinutes)) ||
                     (sessionEndTime > s.StartTime && sessionEndTime <= s.StartTime.AddMinutes(s.MovieDurationInMinutes + CleanUpDurationInMinutes))),
                    cancellationToken);

                if (!hasCollision)
                {
                    decimal calculatedPrice = sessionStartTime.Hour switch
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
                        StartTime = sessionStartTime,
                        MovieDurationInMinutes = currentMovie.DurationInMinutes,
                        OriginalPrice = request.BasePrice,
                        BasePrice = Math.Round(calculatedPrice, 0),
                        Status = SessionStatus.Active
                    };

                    _context.Sessions.Add(newSession);
                    sessionsCreated++;
                }

                var nextTimeWithCleanUp = sessionEndTime.AddMinutes(CleanUpDurationInMinutes);
                int minutesToSubtractOrAdd = nextTimeWithCleanUp.Minute % 5;

                currentTrackTime = minutesToSubtractOrAdd == 0
                    ? nextTimeWithCleanUp
                    : nextTimeWithCleanUp.AddMinutes(5 - minutesToSubtractOrAdd);

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