using StarPlex.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Halls.Commands.UpdateHall;

public class UpdateHallCommandHandler : IRequestHandler<UpdateHallCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateHallCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateHallCommand request, CancellationToken cancellationToken)
    {
        var hall = await _context.Halls
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (hall == null)
        {
            throw new KeyNotFoundException($"Hall with ID '{request.Id}' was not found.");
        }

        if (!_currentUserService.IsSuperAdmin && _currentUserService.CinemaId != hall.CinemaId)
        {
            throw new ForbiddenException("У вас немає прав для редагування залів цього кінотеатру.");
        }

        var trimmedName = request.Name.Trim();

        var isDuplicate = await _context.Halls
            .AnyAsync(h => h.CinemaId == hall.CinemaId
                           && h.Id != request.Id
                           && h.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (isDuplicate)
        {
            throw new ConflictException($"Another hall with name '{trimmedName}' already exists in this cinema.");
        }

        hall.Name = trimmedName;
        hall.TotalRows = request.TotalRows;
        hall.SeatsPerRow = request.SeatsPerRow;
        hall.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
    }
}