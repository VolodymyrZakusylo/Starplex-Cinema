using MediatR;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Movies.Commands.CreateMovie;

public class CreateMovieCommand : IRequest<Guid>
{
    public int TmdbId { get; set; }
    public string? Title { get; set; }
    public string? OriginalTitle { get; set; }
    public string? Description { get; set; }
    public int DurationInMinutes { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? Genre { get; set; }
    public string? TrailerUrl { get; set; }
    public string? AgeRating { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public MovieStatus? Status { get; set; }
    public Stream? PosterFileStream { get; set; }
    public string? PosterFileName { get; set; }
}