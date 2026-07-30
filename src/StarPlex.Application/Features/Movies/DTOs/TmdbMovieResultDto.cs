namespace StarPlex.Application.Features.Movies.DTOs;

public class TmdbMovieResultDto
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
}