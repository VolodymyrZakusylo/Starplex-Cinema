using MediatR;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Halls.Commands.UpdateSeatProperties;

public class UpdateSeatPropertiesCommand : IRequest<Unit>
{
    public Guid SeatId { get; set; }
    public SeatType Type { get; set; }
    public SeatStatus Status { get; set; }
}