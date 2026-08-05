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
            throw new KeyNotFoundException($"Hall with ID '{request.Id}' was not found.");
        }

        if (!_currentUserService.IsSuperAdmin && _currentUserService.CinemaId != hall.CinemaId)
        {
            throw new ForbiddenException("У вас немає прав для видалення залів цього кінотеатру.");
        }

        _context.Halls.Remove(hall);
        await _context.SaveChangesAsync(cancellationToken);
    }
}