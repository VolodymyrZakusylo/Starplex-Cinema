using MediatR;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Movies.DTOs;
using StarPlex.Application.Features.Sessions.DTOs;

namespace StarPlex.Application.Features.Movies.Queries.GetMovieById;

public class GetMovieByIdQueryHandler : IRequestHandler<GetMovieByIdQuery, MovieDto?>
{
    private readonly IApplicationDbContext _context;

    public GetMovieByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MovieDto?> Handle(GetMovieByIdQuery request, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var movie = await _context.Movies
            .Include(m => m.Sessions
                .Where(s => s.Status == StarPlex.Domain.Enums.SessionStatus.Active
                         && s.StartTime >= nowUtc
                         && s.Hall.IsActive))
                .ThenInclude(s => s.Hall)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (movie == null) return null;

        return new MovieDto
        {
            Id = movie.Id,
            TmdbId = movie.TmdbId,
            Title = movie.Title,
            Description = movie.Description,
            DurationInMinutes = movie.DurationInMinutes,
            EffectivePosterUrl = movie.EffectivePosterUrl,
            Genre = movie.Genre,
            TrailerUrl = movie.TrailerUrl,
            AgeRating = movie.AgeRating,
            TmdbRating = movie.TmdbRating,
            Status = movie.Status,
            ReleaseDate = movie.ReleaseDate,
            Slug = movie.Slug,
            BackdropUrl = movie.BackdropUrl,

            Sessions = movie.Sessions.Select(s => new SessionDto
            {
                Id = s.Id,
                MovieId = s.MovieId,
                MovieTitle = movie.Title,
                MoviePosterUrl = movie.EffectivePosterUrl,
                HallId = s.HallId,
                CinemaId = s.Hall != null ? s.Hall.CinemaId : Guid.Empty,
                HallName = s.Hall != null ? s.Hall.Name : "Основний зал",
                StartTime = s.StartTime,
                EndTime = s.StartTime.AddMinutes(movie.DurationInMinutes),
                BasePrice = s.BasePrice,
                Status = s.Status
            }).ToList()
        };
    }
}