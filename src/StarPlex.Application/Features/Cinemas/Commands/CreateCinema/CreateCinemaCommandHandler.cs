using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;

namespace StarPlex.Application.Features.Cinemas.Commands.CreateCinema;

public class CreateCinemaCommandHandler : IRequestHandler<CreateCinemaCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateCinemaCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCinemaCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name.Trim();
        var trimmedAddress = request.Address.Trim();

        var exists = await _context.Cinemas
            .AnyAsync(c => c.Name == trimmedName && c.Address == trimmedAddress, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A cinema with this name and address already exists.");
        }

        var cinema = new Cinema
        {
            Name = trimmedName,
            Address = trimmedAddress,
            City = request.City.Trim()
        };

        _context.Cinemas.Add(cinema);
        await _context.SaveChangesAsync(cancellationToken);

        return cinema.Id;
    }
}