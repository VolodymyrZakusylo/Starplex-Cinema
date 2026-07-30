using MediatR;

namespace StarPlex.Application.Features.Bookings.Commands.CancelTicket;

public class CancelTicketCommand : IRequest<bool>
{
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
}