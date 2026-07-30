using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Movies.DTOs;

namespace StarPlex.Application.Features.Movies.Queries.GetMovies;

public class GetMoviesQueryHandler : IRequestHandler<GetMoviesQuery, List<MovieDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMoviesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MovieDto>> Handle(GetMoviesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Movies.AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }

        var movies = await query.ToListAsync(cancellationToken);

        return movies.Adapt<List<MovieDto>>();
    }
}