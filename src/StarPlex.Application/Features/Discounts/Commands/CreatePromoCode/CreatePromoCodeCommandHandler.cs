using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;

namespace StarPlex.Application.Features.Discounts.Commands.CreatePromoCode;

public class CreatePromoCodeCommandHandler : IRequestHandler<CreatePromoCodeCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreatePromoCodeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Discounts
            .AnyAsync(d => d.Code.ToLower() == request.Code.Trim().ToLower(), cancellationToken);

        if (exists)
            throw new ConflictException("Promo code already exists.");

        var discount = new Discount
        {
            Code = request.Code.ToUpper().Trim(),
            Name = request.Name,
            Percentage = request.Percentage,
            UsageLimit = request.UsageLimit,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsActive = true,
            UsageCount = 0
        };

        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync(cancellationToken);

        return discount.Id;
    }
}