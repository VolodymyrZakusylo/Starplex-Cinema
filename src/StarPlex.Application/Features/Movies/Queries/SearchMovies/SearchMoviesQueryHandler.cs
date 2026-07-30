using MediatR;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Movies.DTOs;

namespace StarPlex.Application.Features.Movies.Queries.SearchMovies;

public class SearchMoviesQueryHandler : IRequestHandler<SearchMoviesQuery, List<TmdbMovieResultDto>>
{
    private readonly ITmdbService _tmdbService;

    public SearchMoviesQueryHandler(ITmdbService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    public async Task<List<TmdbMovieResultDto>> Handle(SearchMoviesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new List<TmdbMovieResultDto>();
        }

        return await _tmdbService.SearchMoviesAsync(request.Query, cancellationToken);
    }
}