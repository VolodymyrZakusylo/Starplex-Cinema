using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Cinemas.DTOs;

namespace StarPlex.Application.Features.Cinemas.Queries.GetCinemaById;

public class GetCinemaByIdQueryHandler : IRequestHandler<GetCinemaByIdQuery, CinemaDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCinemaByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CinemaDto?> Handle(GetCinemaByIdQuery request, CancellationToken cancellationToken)
    {
        var cinema = await _context.Cinemas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cinema == null)
        {
            return null;
        }

        return cinema.Adapt<CinemaDto>();
    }
}