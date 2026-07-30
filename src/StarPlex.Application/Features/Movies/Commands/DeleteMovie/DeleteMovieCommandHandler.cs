using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Movies.Commands.DeleteMovie;

public class DeleteMovieCommandHandler : IRequestHandler<DeleteMovieCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteMovieCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (movie == null)
        {
            throw new KeyNotFoundException($"Movie with ID '{request.Id}' was not found.");
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync(cancellationToken);
    }
}