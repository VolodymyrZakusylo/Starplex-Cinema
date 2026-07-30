namespace StarPlex.Application.Features.Discounts.Queries.GetPromoCodeByCode;

public class PromoCodeValidationResultDto
{
    public bool IsValid { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string Message { get; set; } = string.Empty;
}