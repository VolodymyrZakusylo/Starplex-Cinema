using FluentValidation;

namespace StarPlex.Application.Features.Bookings.Commands.LockSeats;

public class LockSeatsCommandValidator : AbstractValidator<LockSeatsCommand>
{
    public LockSeatsCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.SeatIds).NotEmpty().WithMessage("You must select at least one seat.");
    }
}