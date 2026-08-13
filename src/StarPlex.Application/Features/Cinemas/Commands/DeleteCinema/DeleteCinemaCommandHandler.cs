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
        var cinema = await _context.Cinemas
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cinema == null)
        {
            throw new NotFoundException("Cinema", request.Id);
        }

        _context.Cinemas.Remove(cinema);
        await _context.SaveChangesAsync(cancellationToken);
    }
}