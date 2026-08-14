using FluentValidation;

namespace StarPlex.Application.Features.Bookings.Commands.ScanTicket;

public class ScanTicketCommandValidator : AbstractValidator<ScanTicketCommand>
{
    public ScanTicketCommandValidator()
    {
        RuleFor(x => x.TicketCode).NotEmpty();
    }
}
