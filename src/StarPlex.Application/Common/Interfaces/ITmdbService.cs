using StarPlex.Application.Features.Movies.DTOs;

namespace StarPlex.Application.Common.Interfaces;

public interface ITmdbService
{
    Task<List<TmdbMovieResultDto>> SearchMoviesAsync(string query, CancellationToken cancellationToken);

    Task<TmdbMovieDetailsDto?> GetMovieDetailsAsync(int tmdbId, CancellationToken cancellationToken);
}