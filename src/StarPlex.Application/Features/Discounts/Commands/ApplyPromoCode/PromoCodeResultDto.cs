namespace StarPlex.Application.Features.Discounts.Commands.ApplyPromoCode;

public class PromoCodeResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal NewTotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
}