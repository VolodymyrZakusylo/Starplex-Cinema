using FluentValidation;

namespace StarPlex.Application.Features.Halls.Commands.UpdateSeatProperties;

public class UpdateSeatPropertiesCommandValidator : AbstractValidator<UpdateSeatPropertiesCommand>
{
    public UpdateSeatPropertiesCommandValidator()
    {
        RuleFor(x => x.SeatId)
            .NotEmpty().WithMessage("Seat ID is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("A valid seat type must be provided.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid seat status must be provided.");
    }
}