using StarPlex.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Cinemas.Commands.DeleteCinema;

public class DeleteCinemaCommandHandler : IRequestHandler<DeleteCinemaCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCinemaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCinemaCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (_context.Database.ProviderName?.Contains("Npgsql") == true)
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Cinemas\" WHERE \"Id\" = {request.Id} FOR UPDATE", cancellationToken);

        var cinema = await _context.Cinemas
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cinema == null)
        {
            throw new NotFoundException("Cinema", request.Id);
        }

        const string conflict = "This cinema has historical sessions that must be preserved and cannot be deleted.";
        if (await _context.Sessions.AnyAsync(s => s.Hall.CinemaId == cinema.Id, cancellationToken))
            throw new ConflictException(conflict);
        if (await _context.HasUsersAssignedToCinemaAsync(cinema.Id, cancellationToken))
            throw new ConflictException("Reassign this cinema's staff before deleting it.");

        _context.Cinemas.Remove(cinema);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (_context.IsForeignKeyViolation(ex, "FK_Sessions_Halls_HallId"))
        {
            throw new ConflictException(conflict);
        }
    }
}
