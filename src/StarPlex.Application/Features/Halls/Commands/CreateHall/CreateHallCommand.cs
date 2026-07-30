using MediatR;

namespace StarPlex.Application.Features.Halls.Commands.CreateHall;

public class CreateHallCommand : IRequest<Guid>
{
    public Guid CinemaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SeatsPerRow { get; set; }
}