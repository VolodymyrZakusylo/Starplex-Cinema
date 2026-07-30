using MediatR;
using StarPlex.Domain.Enums;

namespace StarPlex.Application.Features.Movies.Commands.UpdateMovie;

public class UpdateMovieCommand : IRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationInMinutes { get; set; }
    public string PosterUrl { get; set; } = string.Empty;
    public string? BackdropUrl { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string TrailerUrl { get; set; } = string.Empty;
    public string AgeRating { get; set; } = string.Empty;
    public MovieStatus Status { get; set; }
    public Stream? PosterFileStream { get; set; }
    public string? PosterFileName { get; set; }
}