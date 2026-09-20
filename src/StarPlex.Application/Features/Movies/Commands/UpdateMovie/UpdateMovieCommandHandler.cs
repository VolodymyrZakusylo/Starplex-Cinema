using System.Text.RegularExpressions;
using StarPlex.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Application.Features.Movies.Commands.UpdateMovie;

public class UpdateMovieCommandHandler : IRequestHandler<UpdateMovieCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public UpdateMovieCommandHandler(IApplicationDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task Handle(UpdateMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (movie == null)
        {
            throw new NotFoundException("Movie", request.Id);
        }

        if (movie.Title != request.Title.Trim())
        {
            movie.Slug = GenerateSlug(request.Title);
        }

        movie.Title = request.Title.Trim();
        movie.Description = request.Description.Trim();
        movie.DurationInMinutes = request.DurationInMinutes;
        movie.BackdropUrl = request.BackdropUrl?.Trim();
        movie.Genre = request.Genre.Trim();
        movie.TrailerUrl = request.TrailerUrl.Trim();
        movie.AgeRating = request.AgeRating.Trim();
        movie.Status = request.Status; 

        if (request.PosterFileStream != null && !string.IsNullOrEmpty(request.PosterFileName))
        {
            var safeExtension = await StarPlex.Application.Common.Helpers.ImageValidator.ValidateAndGetSafeExtensionAsync(request.PosterFileStream, request.PosterFileName);
            var folderPath = "uploads/posters";
            var uniqueFileName = $"{Guid.NewGuid()}{safeExtension}";

            var savedPath = await _fileStorageService.SaveFileAsync(request.PosterFileStream, folderPath, uniqueFileName, cancellationToken);
            movie.PosterStoragePath = savedPath;
        }
        else if (!string.IsNullOrWhiteSpace(request.PosterUrl))
        {
            movie.PosterUrl = request.PosterUrl.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private string GenerateSlug(string title)
    {
        string slug = title.ToLower().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9а-яёіїєґ\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        return string.IsNullOrEmpty(slug) ? "movie" : slug;
    }
}