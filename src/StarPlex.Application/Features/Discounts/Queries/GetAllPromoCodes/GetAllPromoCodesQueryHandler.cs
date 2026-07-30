using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;

namespace StarPlex.Application.Features.Discounts.Queries.GetAllPromoCodes;

public class GetAllPromoCodesQueryHandler : IRequestHandler<GetAllPromoCodesQuery, IEnumerable<Discount>>
{
    private readonly IApplicationDbContext _context;

    public GetAllPromoCodesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Discount>> Handle(GetAllPromoCodesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Discounts
            .OrderByDescending(d => d.ValidTo)
            .ToListAsync(cancellationToken);
    }
}