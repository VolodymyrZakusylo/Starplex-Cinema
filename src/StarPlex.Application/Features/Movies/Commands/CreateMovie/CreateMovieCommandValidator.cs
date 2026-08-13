using FluentValidation;

namespace StarPlex.Application.Features.Movies.Commands.CreateMovie;

public class CreateMovieCommandValidator : AbstractValidator<CreateMovieCommand>
{
    public CreateMovieCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .When(x => x.TmdbId == 0);
    }
}
