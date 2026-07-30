using MediatR;

namespace StarPlex.Application.Features.Discounts.Commands.DeletePromoCode;

public record DeletePromoCodeCommand(Guid Id) : IRequest<Unit>;