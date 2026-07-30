using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Halls.DTOs;

namespace StarPlex.Application.Features.Halls.Queries.GetHallById;

public class GetHallByIdQueryHandler : IRequestHandler<GetHallByIdQuery, HallDto?>
{
    private readonly IApplicationDbContext _context;

    public GetHallByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HallDto?> Handle(GetHallByIdQuery request, CancellationToken cancellationToken)
    {
        var hall = await _context.Halls
            .AsNoTracking()
            .Include(h => h.Seats)
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (hall == null) return null;

        return hall.Adapt<HallDto>();
    }
}