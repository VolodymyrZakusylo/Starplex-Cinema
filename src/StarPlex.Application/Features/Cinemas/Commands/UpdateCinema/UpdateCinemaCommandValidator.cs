using FluentValidation;

namespace StarPlex.Application.Features.Cinemas.Commands.UpdateCinema;

public class UpdateCinemaCommandValidator : AbstractValidator<UpdateCinemaCommand>
{
    public UpdateCinemaCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Cinema ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Cinema name is required.")
            .MaximumLength(200).WithMessage("Cinema name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");
    }
}