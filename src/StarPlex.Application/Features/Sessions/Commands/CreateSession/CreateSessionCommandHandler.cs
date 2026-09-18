using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Helpers;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StarPlex.Application.Features.Sessions.Commands.CreateSession;

public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext)
            throw new InvalidOperationException("Database context is not compatible with transactions.");

        var hall = await _context.Halls
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == request.HallId, cancellationToken);

        if (hall == null) throw new NotFoundException("Cinema Hall", request.HallId);

        if (!_currentUserService.IsSuperAdmin && _currentUserService.CinemaId != hall.CinemaId)
        {
            throw new ForbiddenException("You do not have permission to manage this cinema.");
        }

        if (!hall.IsActive)
            throw new BusinessRuleException("New sessions cannot be created in an inactive hall.");

        var movie = await _context.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MovieId, cancellationToken);

        if (movie == null) throw new NotFoundException("Movie", request.MovieId);

        var startTimeUtc = request.StartTime.Kind == DateTimeKind.Utc
            ? request.StartTime
            : DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);

        var cleanUpDuration = Session.CleanUpDurationInMinutes;
        var endTimeUtc = startTimeUtc.AddMinutes(movie.DurationInMinutes + cleanUpDuration);

        var kyivTzi = TimeZoneHelpers.KyivTimeZone;
        var startTimeKyiv = TimeZoneInfo.ConvertTimeFromUtc(startTimeUtc, kyivTzi);
        var occupiedEndTimeKyiv = TimeZoneInfo.ConvertTimeFromUtc(endTimeUtc, kyivTzi);

        var startOfWorkingDayKyiv = new DateTime(startTimeKyiv.Year, startTimeKyiv.Month, startTimeKyiv.Day, 10, 0, 0);
        var endOfWorkingDayKyiv = new DateTime(startTimeKyiv.Year, startTimeKyiv.Month, startTimeKyiv.Day, 23, 0, 0);

        if (startTimeKyiv.Date != occupiedEndTimeKyiv.Date ||
            startTimeKyiv < startOfWorkingDayKyiv ||
            occupiedEndTimeKyiv > endOfWorkingDayKyiv)
        {
            throw new BusinessRuleException("Session time falls outside cinema working hours (10:00 - 23:00 Kyiv time).");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var hasCollision = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.HallId == request.HallId && s.Status == SessionStatus.Active)
                .AnyAsync(s => s.StartTime < endTimeUtc &&
                               s.StartTime.AddMinutes(s.MovieDurationInMinutes + Session.CleanUpDurationInMinutes) > startTimeUtc,
                          cancellationToken);

            if (hasCollision)
            {
                throw new ConflictException("Time slot collision detected. This hall is already occupied by another session during the specified time.");
            }

            var session = new Session(
                request.MovieId,
                request.HallId,
                startTimeUtc,
                movie.DurationInMinutes,
                request.BasePrice,
                request.BasePrice,
                SessionStatus.Active
            );

            if (movie.Status == MovieStatus.ComingSoon)
            {
                movie.Status = MovieStatus.NowShowing;
            }

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return session.Id;
    }
}
