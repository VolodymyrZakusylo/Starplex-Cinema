using StarPlex.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Halls.Commands.DeleteHall;

public class DeleteHallCommandHandler : IRequestHandler<DeleteHallCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteHallCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteHallCommand request, CancellationToken cancellationToken)
    {
        var hall = await _context.Halls
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (hall == null)
        {
            throw new NotFoundException("Hall", request.Id);
        }

        if (!_currentUserService.IsSuperAdmin && _currentUserService.CinemaId != hall.CinemaId)
        {
            throw new ForbiddenException("У вас немає прав для видалення залів цього кінотеатру.");
        }

        const string conflict = "This hall has historical sessions that must be preserved. Deactivate the hall instead.";
        if (await _context.Sessions.AnyAsync(s => s.HallId == hall.Id, cancellationToken))
            throw new ConflictException(conflict);

        _context.Halls.Remove(hall);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (_context.IsForeignKeyViolation(ex, "FK_Sessions_Halls_HallId"))
        {
            throw new ConflictException(conflict);
        }
    }
}
