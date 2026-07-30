using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Discounts.Queries.GetPromoCodeByCode;

public class GetPromoCodeByCodeQueryHandler : IRequestHandler<GetPromoCodeByCodeQuery, PromoCodeValidationResultDto>
{
    private readonly IApplicationDbContext _context;

    public GetPromoCodeByCodeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PromoCodeValidationResultDto> Handle(GetPromoCodeByCodeQuery request, CancellationToken cancellationToken)
    {
        var discount = await _context.Discounts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code.ToLower() == request.Code.Trim().ToLower(), cancellationToken);

        var now = DateTime.UtcNow;

        if (discount == null || !discount.IsActive)
        {
            return new PromoCodeValidationResultDto { IsValid = false, Message = "Promo code does not exist or is inactive." };
        }

        if (now < discount.ValidFrom || now > discount.ValidTo)
        {
            return new PromoCodeValidationResultDto { IsValid = false, Message = "This promo code has expired." };
        }

        if (discount.UsageCount >= discount.UsageLimit)
        {
            return new PromoCodeValidationResultDto { IsValid = false, Message = "This promo code has reached its usage limit." };
        }

        return new PromoCodeValidationResultDto
        {
            IsValid = true,
            Code = discount.Code,
            Percentage = discount.Percentage,
            Message = "Promo code is valid."
        };
    }
}