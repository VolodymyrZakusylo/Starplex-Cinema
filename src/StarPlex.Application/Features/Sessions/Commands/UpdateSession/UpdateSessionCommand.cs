using MediatR;

namespace StarPlex.Application.Features.Sessions.Commands.UpdateSession;

public class UpdateSessionCommand : IRequest
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public Guid HallId { get; set; }
    public DateTime StartTime { get; set; }
    public decimal BasePrice { get; set; }
}