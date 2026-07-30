using FluentValidation;

namespace StarPlex.Application.Features.Movies.Commands.UpdateMovie;

public class UpdateMovieCommandValidator : AbstractValidator<UpdateMovieCommand>
{
    public UpdateMovieCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Movie ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(250).WithMessage("Title must not exceed 250 characters.");

        RuleFor(x => x.DurationInMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than 0.");

        RuleFor(x => x.AgeRating)
            .MaximumLength(10).WithMessage("Age rating must not exceed 10 characters.");
    }
}