using MediatR;

namespace StarPlex.Application.Features.Discounts.Commands.DeletePromoCode;

public record DeletePromoCodeCommand(Guid Id) : IRequest<DeletePromoCodeResult>;

public record DeletePromoCodeResult(string Outcome);
