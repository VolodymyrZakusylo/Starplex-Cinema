using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Movies.Commands.CreateMovie;

public class CreateMovieCommandHandler : IRequestHandler<CreateMovieCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITmdbService _tmdbService;
    private readonly IFileStorageService _fileStorageService;

    public CreateMovieCommandHandler(IApplicationDbContext context, ITmdbService tmdbService, IFileStorageService fileStorageService)
    {
        _context = context;
        _tmdbService = tmdbService;
        _fileStorageService = fileStorageService;
    }

    public async Task<Guid> Handle(CreateMovieCommand request, CancellationToken cancellationToken)
    {
        Movie movie;

        if (request.TmdbId > 0)
        {
            var movieExistsInDb = await _context.Movies
                .AnyAsync(m => m.TmdbId == request.TmdbId, cancellationToken);

            if (movieExistsInDb)
            {
                throw new InvalidOperationException("Цей фільм уже імпортовано в систему.");
            }

            var tmdbMovie = await _tmdbService.GetMovieDetailsAsync(request.TmdbId, cancellationToken);

            if (tmdbMovie == null)
            {
                throw new KeyNotFoundException($"Фільм з TMDB ID {request.TmdbId} не знадено.");
            }

            var calculatedStatus = tmdbMovie.ReleaseDate > DateTime.UtcNow
                ? MovieStatus.ComingSoon
                : MovieStatus.NowShowing;

            movie = new Movie(
                tmdbMovie.TmdbId,
                tmdbMovie.Title,
                tmdbMovie.OriginalTitle ?? tmdbMovie.Title,
                tmdbMovie.Description,
                tmdbMovie.DurationInMinutes,
                tmdbMovie.PosterUrl,
                tmdbMovie.BackdropUrl,
                tmdbMovie.Genre,
                tmdbMovie.TrailerUrl,
                tmdbMovie.AgeRating ?? "16+",
                tmdbMovie.TmdbRating,
                calculatedStatus,
                tmdbMovie.ReleaseDate
            );
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new InvalidOperationException("Назва фільму є обов'язковою для ручного введення.");
            }

            var releaseDateUtc = request.ReleaseDate.HasValue
                ? DateTime.SpecifyKind(request.ReleaseDate.Value, DateTimeKind.Utc)
                : DateTime.UtcNow;

            var finalStatus = request.Status.HasValue
                ? request.Status.Value
                : (releaseDateUtc > DateTime.UtcNow ? MovieStatus.ComingSoon : MovieStatus.NowShowing);

            movie = new Movie(
                0,
                request.Title.Trim(),
                request.OriginalTitle?.Trim() ?? request.Title.Trim(),
                request.Description?.Trim() ?? string.Empty,
                request.DurationInMinutes,
                request.PosterUrl?.Trim() ?? string.Empty,
                request.BackdropUrl?.Trim() ?? string.Empty,
                request.Genre?.Trim() ?? "Невідомо",
                request.TrailerUrl?.Trim() ?? string.Empty,
                request.AgeRating ?? "16+",
                0.0,
                finalStatus,
                releaseDateUtc
            );

            if (request.PosterFileStream != null && !string.IsNullOrEmpty(request.PosterFileName))
            {
                var folderPath = "uploads/posters";
                var uniqueFileName = $"{Guid.NewGuid()}_{request.PosterFileName}";

                var savedPath = await _fileStorageService.SaveFileAsync(request.PosterFileStream, folderPath, uniqueFileName, cancellationToken);
                movie.PosterStoragePath = savedPath;
            }
        }

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync(cancellationToken);

        return movie.Id;
    }
}