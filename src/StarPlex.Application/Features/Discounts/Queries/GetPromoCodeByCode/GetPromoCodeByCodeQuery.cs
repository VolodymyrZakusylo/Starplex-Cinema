using MediatR;

namespace StarPlex.Application.Features.Discounts.Queries.GetPromoCodeByCode;

public record GetPromoCodeByCodeQuery(string Code) : IRequest<PromoCodeValidationResultDto>;