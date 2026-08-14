using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Cinemas.DTOs;

namespace StarPlex.Application.Features.Cinemas.Queries.GetCinemas;

public class GetCinemasQueryHandler : IRequestHandler<GetCinemasQuery, List<CinemaDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCinemasQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CinemaDto>> Handle(GetCinemasQuery request, CancellationToken cancellationToken)
    {
        return await _context.Cinemas
            .AsNoTracking()
            .ProjectToType<CinemaDto>()
            .ToListAsync(cancellationToken);
    }
}