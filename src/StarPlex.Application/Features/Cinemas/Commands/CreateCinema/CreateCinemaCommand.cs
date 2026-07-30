using MediatR;

namespace StarPlex.Application.Features.Cinemas.Commands.CreateCinema;

public class CreateCinemaCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}