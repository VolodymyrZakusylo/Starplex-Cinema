using StarPlex.Application.Features.Sessions.DTOs;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Movies.DTOs;

public class MovieDto
{
    public Guid Id { get; set; }
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationInMinutes { get; set; }
    public string EffectivePosterUrl { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string TrailerUrl { get; set; } = string.Empty;
    public string AgeRating { get; set; } = string.Empty;
    public double TmdbRating { get; set; }
    public MovieStatus Status { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? BackdropUrl { get; set; }

    public List<SessionDto> Sessions { get; set; } = new();
}