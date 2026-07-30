using FluentValidation;

namespace StarPlex.Application.Features.Bookings.Commands.CreateCashierSale;

public class CreateCashierSaleCommandValidator : AbstractValidator<CreateCashierSaleCommand>
{
    public CreateCashierSaleCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.SeatIds)
            .NotEmpty().WithMessage("At least one seat must be selected for the sale.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required.")
            .Must(method => method == "Cash" || method == "Card")
            .WithMessage("Payment method must be either 'Cash' or 'Card'.");
    }
}