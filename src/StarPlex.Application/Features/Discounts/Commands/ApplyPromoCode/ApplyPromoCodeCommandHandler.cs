using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;
using StarPlex.Domain.Services;

namespace StarPlex.Application.Features.Discounts.Commands.ApplyPromoCode;

public class ApplyPromoCodeCommandHandler : IRequestHandler<ApplyPromoCodeCommand, PromoCodeResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ApplyPromoCodeCommandHandler> _logger;

    public ApplyPromoCodeCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<ApplyPromoCodeCommandHandler> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<PromoCodeResultDto> Handle(ApplyPromoCodeCommand request, CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext)
            return new PromoCodeResultDto { IsSuccess = false, Message = "Database context error." };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (dbContext.Database.ProviderName?.Contains("Npgsql") == true)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Bookings\" WHERE \"Id\" = {request.BookingId} FOR UPDATE", cancellationToken);
        }

        var booking = await _context.Bookings
            .Include(b => b.Discount)
            .Include(b => b.Payment)
            .Include(b => b.Session)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
            return new PromoCodeResultDto { IsSuccess = false, Message = "Booking not found." };

        if (request.UserId == Guid.Empty || booking.UserId != request.UserId)
            return new PromoCodeResultDto { IsSuccess = false, Message = "You do not have permission to modify this booking." };

        if (booking.Status != BookingStatus.Pending)
            return new PromoCodeResultDto { IsSuccess = false, Message = "Promo code can only be applied to a pending booking." };

        if (booking.Payment == null || booking.Payment.Status != PaymentStatus.Pending || string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId))
            return new PromoCodeResultDto { IsSuccess = false, Message = "A valid pending payment is required to apply a promo code." };

        if (booking.DiscountId != null)
            return new PromoCodeResultDto { IsSuccess = false, Message = "A promo code has already been applied to this booking." };

        var discount = await _context.Discounts
            .FirstOrDefaultAsync(d => d.Code.ToLower() == request.PromoCode.Trim().ToLower(), cancellationToken);

        if (discount == null || !discount.IsActive)
            return new PromoCodeResultDto { IsSuccess = false, Message = "This promo code does not exist or is inactive." };

        if (dbContext.Database.ProviderName?.Contains("Npgsql") == true)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Discounts\" WHERE \"Id\" = {discount.Id} FOR UPDATE", cancellationToken);
            await dbContext.Entry(discount).ReloadAsync(cancellationToken);
        }

        var now = DateTime.UtcNow;
        if (now < discount.ValidFrom || now > discount.ValidTo)
            return new PromoCodeResultDto { IsSuccess = false, Message = "This promo code has expired or is not yet active." };

        if (discount.UsageCount >= discount.UsageLimit)
            return new PromoCodeResultDto { IsSuccess = false, Message = "This promo code has reached its usage limit." };

        decimal discountAmount = PricingCalculator.CalculateDiscountAmount(booking.TotalPrice, discount.Percentage);
        decimal newTotalPrice = booking.TotalPrice - discountAmount;

        bool stripeUpdated = await _paymentService.UpdatePaymentIntentAmountAsync(
            booking.Payment.StripePaymentIntentId,
            newTotalPrice,
            "uah",
            cancellationToken);

        if (!stripeUpdated)
        {
            return new PromoCodeResultDto
            {
                IsSuccess = false,
                Message = "Failed to update payment amount with payment provider."
            };
        }

        booking.DiscountId = discount.Id;
        booking.TotalPrice = newTotalPrice;
        booking.Payment.Amount = newTotalPrice;

        var bookingSeatsList = booking.BookingSeats.ToList();
        if (bookingSeatsList.Any())
        {
            decimal totalBasePrice = bookingSeatsList.Sum(bs =>
                PricingCalculator.CalculateTicketPrice(booking.Session.BasePrice, bs.Seat.Type));

            decimal accumulatedPurchasePrice = 0;
            for (int i = 0; i < bookingSeatsList.Count; i++)
            {
                var bs = bookingSeatsList[i];
                decimal seatBasePrice = PricingCalculator.CalculateTicketPrice(booking.Session.BasePrice, bs.Seat.Type);

                decimal seatPurchasePrice;
                if (i == bookingSeatsList.Count - 1)
                {
                    seatPurchasePrice = newTotalPrice - accumulatedPurchasePrice;
                }
                else
                {
                    decimal ratio = totalBasePrice > 0 ? newTotalPrice / totalBasePrice : 0;
                    seatPurchasePrice = Math.Round(seatBasePrice * ratio, 2, MidpointRounding.AwayFromZero);
                    accumulatedPurchasePrice += seatPurchasePrice;
                }

                bs.SetPurchasePrice(seatPurchasePrice);
            }
        }

        discount.UsageCount++;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save database changes after updating Stripe PaymentIntent {PaymentIntentId} for booking {BookingId}", booking.Payment.StripePaymentIntentId, booking.Id);
            throw;
        }

        return new PromoCodeResultDto
        {
            IsSuccess = true,
            Message = $"Promo code applied successfully! {discount.Percentage}% discount code activated.",
            NewTotalPrice = newTotalPrice,
            DiscountAmount = discountAmount
        };
    }
}