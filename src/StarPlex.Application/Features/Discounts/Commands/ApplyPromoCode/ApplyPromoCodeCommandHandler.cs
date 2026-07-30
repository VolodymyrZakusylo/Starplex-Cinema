using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Discounts.Commands.ApplyPromoCode;

public class ApplyPromoCodeCommandHandler : IRequestHandler<ApplyPromoCodeCommand, PromoCodeResultDto>
{
    private readonly IApplicationDbContext _context;

    public ApplyPromoCodeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PromoCodeResultDto> Handle(ApplyPromoCodeCommand request, CancellationToken cancellationToken)
    {

        var booking = await _context.Bookings
            .Include(b => b.Discount)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
            return new PromoCodeResultDto { IsSuccess = false, Message = "Booking not found." };

        if (booking.DiscountId != null)
            return new PromoCodeResultDto { IsSuccess = false, Message = "A promo code has already been applied to this booking." };

        var discount = await _context.Discounts
            .FirstOrDefaultAsync(d => d.Code.ToLower() == request.PromoCode.Trim().ToLower(), cancellationToken);

        if (discount == null || !discount.IsActive)
            return new PromoCodeResultDto { IsSuccess = false, Message = "This promo code does not exist or is inactive." };

        var now = DateTime.UtcNow;
        if (now < discount.ValidFrom || now > discount.ValidTo)
            return new PromoCodeResultDto { IsSuccess = false, Message = "This promo code has expired or is not yet active." };

        if (discount.UsageCount >= discount.UsageLimit)
            return new PromoCodeResultDto { IsSuccess = false, Message = "This promo code has reached its usage limit." };

        decimal discountFactor = discount.Percentage / 100;
        decimal discountAmount = Math.Round(booking.TotalPrice * discountFactor, 2);
        decimal newTotalPrice = booking.TotalPrice - discountAmount;

        booking.DiscountId = discount.Id;
        booking.TotalPrice = newTotalPrice;
        discount.UsageCount++;

        await _context.SaveChangesAsync(cancellationToken);

        return new PromoCodeResultDto
        {
            IsSuccess = true,
            Message = $"Promo code applied successfully! {discount.Percentage}% discount code activated.",
            NewTotalPrice = newTotalPrice,
            DiscountAmount = discountAmount
        };
    }
}