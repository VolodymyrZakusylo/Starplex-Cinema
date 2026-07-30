using MediatR;

namespace StarPlex.Application.Features.Cinemas.Commands.DeleteCinema;

public class DeleteCinemaCommand : IRequest
{
    public Guid Id { get; set; }
}