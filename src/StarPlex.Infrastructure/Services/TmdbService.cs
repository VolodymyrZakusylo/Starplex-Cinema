using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Movies.DTOs;

namespace StarPlex.Infrastructure.Services;

public class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public TmdbService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["TmdbSettings:ApiKey"]
              ?? configuration.GetSection("TmdbSettings")["ApiKey"]
              ?? throw new ArgumentNullException("TMDB API Key is missing in appsettings.json");
    }

    public async Task<List<TmdbMovieResultDto>> SearchMoviesAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<TmdbMovieResultDto>();

        var url = $"search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&language=uk-UA&page=1";

        var response = await _httpClient.GetFromJsonAsync<TmdbSearchResponse>(url, cancellationToken);
        if (response?.Results == null) return new List<TmdbMovieResultDto>();

        return response.Results.Select(m => new TmdbMovieResultDto
        {
            TmdbId = m.Id,
            Title = m.Title,
            ReleaseDate = m.ReleaseDate ?? string.Empty,
            PosterUrl = string.IsNullOrEmpty(m.PosterPath)
                ? string.Empty
                : $"https://image.tmdb.org/t/p/w500{m.PosterPath}"
        }).ToList();
    }

    public async Task<TmdbMovieDetailsDto?> GetMovieDetailsAsync(int tmdbId, CancellationToken cancellationToken)
    {
        var url = $"movie/{tmdbId}?api_key={_apiKey}&language=uk-UA&append_to_response=videos,release_dates";

        try
        {
            var movie = await _httpClient.GetFromJsonAsync<TmdbMovieDetailResponse>(url, cancellationToken);
            if (movie == null) return null;

            var trailerKey = movie.VideosResponse?.Videos?
                .FirstOrDefault(v => v.Site.ToLower() == "youtube" && v.Type.ToLower() == "trailer")?.Key;

            var trailerUrl = string.IsNullOrEmpty(trailerKey)
                ? string.Empty
                : $"https://www.youtube.com/watch?v={trailerKey}";

            var genres = movie.Genres != null
                ? string.Join(", ", movie.Genres.Select(g => g.Name))
                : string.Empty;

            var ageRating = movie.ReleaseDatesResponse?.Results?
                .FirstOrDefault(r => r.IsoCode.ToUpper() == "UA")?.ReleaseDates?
                .FirstOrDefault(d => !string.IsNullOrEmpty(d.Certification))?.Certification;

            if (string.IsNullOrEmpty(ageRating))
            {
                ageRating = movie.ReleaseDatesResponse?.Results?
                    .FirstOrDefault(r => r.IsoCode.ToUpper() == "US")?.ReleaseDates?
                    .FirstOrDefault(d => !string.IsNullOrEmpty(d.Certification))?.Certification ?? "16+";
            }

            if (int.TryParse(ageRating, out _)) ageRating += "+";

            return new TmdbMovieDetailsDto
            {
                TmdbId = movie.Id,
                Title = movie.Title,
                OriginalTitle = movie.OriginalTitle,
                Description = movie.Overview ?? string.Empty,
                DurationInMinutes = movie.Runtime ?? 0,
                PosterUrl = string.IsNullOrEmpty(movie.PosterPath)
                    ? string.Empty
                    : $"https://image.tmdb.org/t/p/w500{movie.PosterPath}",
                BackdropUrl = string.IsNullOrEmpty(movie.BackdropPath)
                    ? string.Empty
                    : $"https://image.tmdb.org/t/p/original{movie.BackdropPath}",
                Genre = genres,
                TrailerUrl = trailerUrl,
                AgeRating = ageRating,
                TmdbRating = Math.Round(movie.VoteAverage, 1),
                ReleaseDate = DateTime.TryParse(movie.ReleaseDate, out var date)
                    ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                    : DateTime.UtcNow
            };
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private class TmdbSearchResponse
    {
        [JsonPropertyName("results")] public List<TmdbMovieResult> Results { get; set; } = new();
    }

    private class TmdbMovieResult
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("poster_path")] public string? PosterPath { get; set; }
        [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
    }

    private class TmdbMovieDetailResponse
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("original_title")] public string OriginalTitle { get; set; } = string.Empty;
        [JsonPropertyName("overview")] public string? Overview { get; set; }
        [JsonPropertyName("runtime")] public int? Runtime { get; set; }
        [JsonPropertyName("poster_path")] public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")] public string? BackdropPath { get; set; }
        [JsonPropertyName("vote_average")] public double VoteAverage { get; set; }
        [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
        [JsonPropertyName("genres")] public List<TmdbGenre>? Genres { get; set; }
        [JsonPropertyName("videos")] public TmdbVideosResponse? VideosResponse { get; set; }
        [JsonPropertyName("release_dates")] public TmdbReleaseDatesResponse? ReleaseDatesResponse { get; set; }
    }

    private class TmdbGenre { [JsonPropertyName("name")] public string Name { get; set; } = string.Empty; }
    private class TmdbVideosResponse { [JsonPropertyName("results")] public List<TmdbVideo>? Videos { get; set; } }
    private class TmdbVideo
    {
        [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;
        [JsonPropertyName("site")] public string Site { get; set; } = string.Empty;
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    }

    private class TmdbReleaseDatesResponse
    {
        [JsonPropertyName("results")] public List<TmdbReleaseCountryResult>? Results { get; set; }
    }

    private class TmdbReleaseCountryResult
    {
        [JsonPropertyName("iso_3166_1")] public string IsoCode { get; set; } = string.Empty;
        [JsonPropertyName("release_dates")] public List<TmdbReleaseDateItem>? ReleaseDates { get; set; }
    }

    private class TmdbReleaseDateItem
    {
        [JsonPropertyName("certification")] public string Certification { get; set; } = string.Empty;
    }
}