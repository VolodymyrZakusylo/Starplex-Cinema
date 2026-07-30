using MediatR;
using StarPlex.Application.Features.Movies.DTOs;

namespace StarPlex.Application.Features.Movies.Queries.GetMovieById;

public class GetMovieByIdQuery : IRequest<MovieDto?>
{
    public Guid Id { get; set; }
}