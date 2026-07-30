using MediatR;

namespace StarPlex.Application.Features.Movies.Commands.DeleteMovie;

public class DeleteMovieCommand : IRequest
{
    public Guid Id { get; set; }
}