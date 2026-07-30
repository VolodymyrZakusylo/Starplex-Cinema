using MediatR;

namespace StarPlex.Application.Features.Halls.Commands.DeleteHall;

public class DeleteHallCommand : IRequest
{
    public Guid Id { get; set; }
}