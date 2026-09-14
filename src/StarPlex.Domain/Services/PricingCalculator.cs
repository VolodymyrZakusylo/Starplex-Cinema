using System;
using StarPlex.Domain.Enums;

namespace StarPlex.Domain.Services;

public static class PricingCalculator
{
    public static decimal GetSeatMultiplier(SeatType type)
    {
        return type switch
        {
            SeatType.VIP => 1.5m,
            SeatType.Disabled => 0.8m,
            _ => 1.0m
        };
    }

    public static decimal CalculateTicketPrice(decimal basePrice, SeatType type)
    {
        return basePrice * GetSeatMultiplier(type);
    }

    public static decimal CalculateDiscountAmount(decimal totalAmount, decimal discountPercentage)
    {
        decimal discountFactor = discountPercentage / 100m;
        return Math.Round(totalAmount * discountFactor, 2);
    }

    public static decimal ApplyDiscount(decimal totalAmount, decimal discountPercentage)
    {
        return totalAmount - CalculateDiscountAmount(totalAmount, discountPercentage);
    }
}
