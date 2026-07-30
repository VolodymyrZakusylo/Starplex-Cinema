namespace StarPlex.Application.Features.Movies.DTOs;

public class TmdbMovieDetailsDto
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationInMinutes { get; set; }
    public string PosterUrl { get; set; } = string.Empty;
    public string? BackdropUrl { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string TrailerUrl { get; set; } = string.Empty;
    public string AgeRating { get; set; } = string.Empty;
    public double TmdbRating { get; set; }
    public DateTime ReleaseDate { get; set; }
}