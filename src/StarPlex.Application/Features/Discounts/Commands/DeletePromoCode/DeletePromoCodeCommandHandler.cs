using MediatR;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Discounts.Commands.DeletePromoCode;

public class DeletePromoCodeCommandHandler : IRequestHandler<DeletePromoCodeCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public DeletePromoCodeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeletePromoCodeCommand request, CancellationToken cancellationToken)
    {
        var discount = await _context.Discounts.FindAsync(new object[] { request.Id }, cancellationToken);

        if (discount == null)
            throw new Exception("Promo code not found.");

        _context.Discounts.Remove(discount);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}