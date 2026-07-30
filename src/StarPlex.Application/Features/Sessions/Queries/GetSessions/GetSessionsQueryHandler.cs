using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Sessions.DTOs;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Sessions.Queries.GetSessions;

public class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, List<SessionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSessionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SessionDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .AsNoTracking();

        query = query.Where(s => s.Status == SessionStatus.Active);

        if (request.MovieId.HasValue)
        {
            query = query.Where(s => s.MovieId == request.MovieId.Value);
        }

        if (request.Date.HasValue)
        {
            var targetDate = DateTime.SpecifyKind(request.Date.Value.Date, DateTimeKind.Utc);
            var nextDate = targetDate.AddDays(1);

            var now = DateTime.UtcNow;

            if (targetDate == now.Date)
            {
                query = query.Where(s => s.StartTime >= now && s.StartTime < nextDate);
            }
            else
            {
                query = query.Where(s => s.StartTime >= targetDate && s.StartTime < nextDate);
            }
        }
        else
        {
            query = query.Where(s => s.StartTime >= DateTime.UtcNow);
        }

        var sessions = await query.OrderBy(s => s.StartTime).ToListAsync(cancellationToken);

        return sessions.Select(s => new SessionDto
        {
            Id = s.Id,
            MovieId = s.MovieId,
            MovieTitle = s.Movie.Title,
            MoviePosterUrl = s.Movie.EffectivePosterUrl,
            HallId = s.HallId,
            HallName = s.Hall.Name,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            BasePrice = s.BasePrice,
            Status = s.Status
        }).ToList();
    }
}