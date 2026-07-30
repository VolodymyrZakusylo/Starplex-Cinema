using MediatR;

namespace StarPlex.Application.Features.Halls.Commands.UpdateHall;

public class UpdateHallCommand : IRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SeatsPerRow { get; set; }
    public bool IsActive { get; set; }
}