using FluentValidation;

namespace StarPlex.Application.Features.Discounts.Commands.ApplyPromoCode;

public class ApplyPromoCodeCommandValidator : AbstractValidator<ApplyPromoCodeCommand>
{
    public ApplyPromoCodeCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("Booking ID is required.");

        RuleFor(x => x.PromoCode)
            .NotEmpty().WithMessage("Promo code cannot be empty.")
            .MaximumLength(50).WithMessage("Promo code must not exceed 50 characters.");
    }
}