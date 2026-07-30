using FluentValidation;

namespace StarPlex.Application.Features.Halls.Commands.CreateHall;

public class CreateHallCommandValidator : AbstractValidator<CreateHallCommand>
{
    public CreateHallCommandValidator()
    {
        RuleFor(x => x.CinemaId)
            .NotEmpty().WithMessage("Cinema ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hall name is required.")
            .MaximumLength(100).WithMessage("Hall name must not exceed 100 characters.");

        RuleFor(x => x.TotalRows)
            .GreaterThan(0).WithMessage("Total rows must be greater than 0.")
            .LessThanOrEqualTo(50).WithMessage("Total rows must not exceed 50.");

        RuleFor(x => x.SeatsPerRow)
            .GreaterThan(0).WithMessage("Seats per row must be greater than 0.")
            .LessThanOrEqualTo(50).WithMessage("Seats per row must not exceed 50.");
    }
}