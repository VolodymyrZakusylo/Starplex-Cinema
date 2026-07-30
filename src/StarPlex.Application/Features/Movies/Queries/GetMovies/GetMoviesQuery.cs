using MediatR;
using StarPlex.Domain.Enums;
using StarPlex.Application.Features.Movies.DTOs;

namespace StarPlex.Application.Features.Movies.Queries.GetMovies;

public class GetMoviesQuery : IRequest<List<MovieDto>>
{
    public MovieStatus? Status { get; set; }
}