using MediatR;
using StarPlex.Domain.Entities;

namespace StarPlex.Application.Features.Discounts.Queries.GetAllPromoCodes;

public record GetAllPromoCodesQuery : IRequest<IEnumerable<Discount>>;