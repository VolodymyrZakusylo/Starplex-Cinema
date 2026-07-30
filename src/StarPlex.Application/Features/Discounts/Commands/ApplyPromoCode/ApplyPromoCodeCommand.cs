using MediatR;

namespace StarPlex.Application.Features.Discounts.Commands.ApplyPromoCode;

public class ApplyPromoCodeCommand : IRequest<PromoCodeResultDto>
{
    public Guid BookingId { get; set; }
    public string PromoCode { get; set; } = string.Empty;
}