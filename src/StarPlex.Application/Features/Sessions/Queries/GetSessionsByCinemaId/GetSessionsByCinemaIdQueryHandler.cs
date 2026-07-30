using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Sessions.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StarPlex.Application.Features.Sessions.Queries.GetSessionsByCinemaId;

public class GetSessionsByCinemaIdQueryHandler : IRequestHandler<GetSessionsByCinemaIdQuery, List<SessionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSessionsByCinemaIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SessionDto>> Handle(GetSessionsByCinemaIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Hall)
            .Where(s => s.Hall.CinemaId == request.CinemaId)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                MovieId = s.MovieId,
                MovieTitle = s.Movie.Title,
                MoviePosterUrl = s.Movie.PosterUrl,
                MovieDurationInMinutes = s.MovieDurationInMinutes,
                HallId = s.HallId,
                HallName = s.Hall.Name,
                StartTime = s.StartTime,
                EndTime = s.StartTime.AddMinutes(s.MovieDurationInMinutes),
                BasePrice = s.BasePrice,
                Status = s.Status
            })
            .ToListAsync(cancellationToken);
    }
}