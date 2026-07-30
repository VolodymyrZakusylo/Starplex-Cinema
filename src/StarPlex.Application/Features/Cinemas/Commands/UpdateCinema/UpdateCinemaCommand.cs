using MediatR;

namespace StarPlex.Application.Features.Cinemas.Commands.UpdateCinema;

public class UpdateCinemaCommand : IRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}