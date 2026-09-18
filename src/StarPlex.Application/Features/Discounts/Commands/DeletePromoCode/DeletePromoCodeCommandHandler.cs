using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Discounts.Commands.DeletePromoCode;

public class DeletePromoCodeCommandHandler : IRequestHandler<DeletePromoCodeCommand, DeletePromoCodeResult>
{
    private readonly IApplicationDbContext _context;

    public DeletePromoCodeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeletePromoCodeResult> Handle(DeletePromoCodeCommand request, CancellationToken cancellationToken)
    {
        var discount = await _context.Discounts.FindAsync(new object[] { request.Id }, cancellationToken);

        if (discount == null)
            throw new NotFoundException("Promo code", request.Id);

        if (await _context.Bookings.AnyAsync(b => b.DiscountId == discount.Id, cancellationToken))
        {
            discount.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
            return new DeletePromoCodeResult("Deactivated");
        }

        _context.Discounts.Remove(discount);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return new DeletePromoCodeResult("Deleted");
        }
        catch (DbUpdateException ex) when (_context.IsForeignKeyViolation(ex, "FK_Bookings_Discounts_DiscountId"))
        {
            _context.Discounts.Entry(discount).State = EntityState.Unchanged;
            discount.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
            return new DeletePromoCodeResult("Deactivated");
        }
    }
}
