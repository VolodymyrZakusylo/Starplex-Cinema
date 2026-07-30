using System.Text.RegularExpressions;
using StarPlex.Domain.Common;
using StarPlex.Domain.Enums;

namespace StarPlex.Domain.Entities;

public class Movie : BaseEntity
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationInMinutes { get; set; }
    public string PosterUrl { get; set; } = string.Empty;
    public string? BackdropUrl { get; set; }
    public string? PosterStoragePath { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string TrailerUrl { get; set; } = string.Empty;
    public string AgeRating { get; set; } = string.Empty;
    public double TmdbRating { get; set; }
    public MovieStatus Status { get; set; }
    public DateTime ReleaseDate { get; set; }

    public ICollection<Session> Sessions { get; set; } = new List<Session>();

    public string EffectivePosterUrl => !string.IsNullOrEmpty(PosterStoragePath)
        ? PosterStoragePath
        : PosterUrl;

    public Movie()
    {
    }

    public Movie(
        int tmdbId,
        string title,
        string originalTitle,
        string description,
        int durationInMinutes,
        string posterUrl,
        string? backdropUrl,
        string genre,
        string trailerUrl,
        string ageRating,
        double tmdbRating,
        MovieStatus status,
        DateTime releaseDate)
    {
        TmdbId = tmdbId;
        Title = title;
        Slug = GenerateSlug(originalTitle);
        Description = description;
        DurationInMinutes = durationInMinutes;
        PosterUrl = posterUrl;
        BackdropUrl = backdropUrl;
        Genre = genre;
        TrailerUrl = trailerUrl;
        AgeRating = ageRating;
        TmdbRating = tmdbRating;
        Status = status;
        ReleaseDate = releaseDate.Kind == DateTimeKind.Utc
            ? releaseDate
            : DateTime.SpecifyKind(releaseDate, DateTimeKind.Utc);
    }

    private string GenerateSlug(string originalTitle)
    {
        string slug = originalTitle.ToLower().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", "-");
        return slug.Trim('-');
    }
}