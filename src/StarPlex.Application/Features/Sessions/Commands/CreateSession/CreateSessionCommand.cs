using MediatR;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Sessions.Commands.CreateSession;

public class CreateSessionCommand : IRequest<Guid>
{
    public Guid MovieId { get; set; }
    public Guid HallId { get; set; }
    public DateTime StartTime { get; set; }
    public decimal BasePrice { get; set; }
}