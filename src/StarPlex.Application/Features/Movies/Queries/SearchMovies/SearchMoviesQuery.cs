using MediatR;
using StarPlex.Application.Features.Movies.DTOs;

namespace StarPlex.Application.Features.Movies.Queries.SearchMovies;

public class SearchMoviesQuery : IRequest<List<TmdbMovieResultDto>>
{
    public string Query { get; set; } = string.Empty;
}