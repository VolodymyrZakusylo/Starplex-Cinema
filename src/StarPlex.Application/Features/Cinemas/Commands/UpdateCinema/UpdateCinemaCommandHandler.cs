using StarPlex.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Cinemas.Commands.UpdateCinema;

public class UpdateCinemaCommandHandler : IRequestHandler<UpdateCinemaCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCinemaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateCinemaCommand request, CancellationToken cancellationToken)
    {
        var cinema = await _context.Cinemas
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cinema == null)
        {
            throw new KeyNotFoundException($"Cinema with ID '{request.Id}' was not found.");
        }

        var trimmedName = request.Name.Trim();
        var trimmedAddress = request.Address.Trim();

        var isDuplicate = await _context.Cinemas
            .AnyAsync(c => c.Id != request.Id && c.Name == trimmedName && c.Address == trimmedAddress, cancellationToken);

        if (isDuplicate)
        {
            throw new ConflictException("Another cinema with this name and address already exists.");
        }

        cinema.Name = trimmedName;
        cinema.Address = trimmedAddress;
        cinema.City = request.City.Trim();

        await _context.SaveChangesAsync(cancellationToken);
    }
}