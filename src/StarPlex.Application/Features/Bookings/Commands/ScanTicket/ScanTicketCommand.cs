using MediatR;

namespace StarPlex.Application.Features.Bookings.Commands.ScanTicket;

public class ScanTicketCommand : IRequest<ScanTicketResultDto>
{
    public string TicketCode { get; set; } = string.Empty;
}