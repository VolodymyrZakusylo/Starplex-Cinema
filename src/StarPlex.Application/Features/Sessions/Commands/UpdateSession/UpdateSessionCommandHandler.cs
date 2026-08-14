using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Sessions.Commands.UpdateSession;

public class UpdateSessionCommandHandler : IRequestHandler<UpdateSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (session == null) throw new NotFoundException("Session", request.Id);

        var targetHall = await _context.Halls
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == request.HallId, cancellationToken);

        if (targetHall == null) throw new NotFoundException("Cinema Hall", request.HallId);

        if (!_currentUserService.IsSuperAdmin &&
            (_currentUserService.CinemaId != session.Hall.CinemaId || _currentUserService.CinemaId != targetHall.CinemaId))
        {
            throw new ForbiddenException("You do not have permission to manage this cinema.");
        }

        var hasBookings = await _context.Bookings
            .AnyAsync(b => b.SessionId == request.Id && b.Status == BookingStatus.Confirmed, cancellationToken);

        var startTimeUtc = request.StartTime.Kind == DateTimeKind.Utc
            ? request.StartTime
            : DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);

        if (hasBookings && (session.StartTime != startTimeUtc || session.HallId != request.HallId))
        {
            throw new BusinessRuleException("Cannot change session time or hall because there are active bookings for this session.");
        }

        if (session.StartTime != startTimeUtc || session.HallId != request.HallId)
        {
            int duration = session.MovieDurationInMinutes;
            if (session.MovieId != request.MovieId)
            {
                var newMovie = await _context.Movies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == request.MovieId, cancellationToken);

                if (newMovie == null) throw new NotFoundException("Movie", request.MovieId);
                duration = newMovie.DurationInMinutes;
                session.MovieId = request.MovieId;
                session.MovieDurationInMinutes = duration;
            }

            var endTimeUtc = startTimeUtc.AddMinutes(duration + Domain.Entities.Session.CleanUpDurationInMinutes);

            var hasCollision = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.HallId == request.HallId && s.Id != session.Id && s.Status == SessionStatus.Active)
                .AnyAsync(s => s.StartTime < endTimeUtc &&
                               s.StartTime.AddMinutes(s.MovieDurationInMinutes + Domain.Entities.Session.CleanUpDurationInMinutes) > startTimeUtc,
                          cancellationToken);

            if (hasCollision)
            {
                throw new ConflictException("Time slot collision detected. The target hall is occupied by another session during this time.");
            }

            session.StartTime = startTimeUtc;
            session.HallId = request.HallId;
        }
        else if (session.MovieId != request.MovieId)
        {
            var newMovie = await _context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.MovieId, cancellationToken);

            if (newMovie == null) throw new NotFoundException("Movie", request.MovieId);
            session.MovieId = request.MovieId;
            session.MovieDurationInMinutes = newMovie.DurationInMinutes;
        }

        session.BasePrice = request.BasePrice;
        await _context.SaveChangesAsync(cancellationToken);
    }
}