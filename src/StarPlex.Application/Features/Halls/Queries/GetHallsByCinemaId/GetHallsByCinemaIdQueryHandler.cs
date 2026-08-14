using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Halls.DTOs;

namespace StarPlex.Application.Features.Halls.Queries.GetHallsByCinemaId;

public class GetHallsByCinemaIdQueryHandler : IRequestHandler<GetHallsByCinemaIdQuery, List<HallDto>>
{
    private readonly IApplicationDbContext _context;

    public GetHallsByCinemaIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<HallDto>> Handle(GetHallsByCinemaIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Halls
            .AsNoTracking()
            .Where(h => h.CinemaId == request.CinemaId)
            .ProjectToType<HallDto>()
            .ToListAsync(cancellationToken);
    }
}