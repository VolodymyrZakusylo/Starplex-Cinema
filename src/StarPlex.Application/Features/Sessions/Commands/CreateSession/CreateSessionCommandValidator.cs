using FluentValidation;

namespace StarPlex.Application.Features.Sessions.Commands.CreateSession;

public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.MovieId).NotEmpty();
        RuleFor(x => x.HallId).NotEmpty();

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("Session start time must be in the future.");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Ticket price must be greater than 0.");
    }
}