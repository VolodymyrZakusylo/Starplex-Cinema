using FluentValidation;

namespace StarPlex.Application.Features.Bookings.Commands.UnlockSeats;

public class UnlockSeatsCommandValidator : AbstractValidator<UnlockSeatsCommand>
{
    public UnlockSeatsCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Session ID is required.");
        RuleFor(x => x.SeatIds).NotEmpty().WithMessage("At least one Seat ID must be provided.");
    }
}