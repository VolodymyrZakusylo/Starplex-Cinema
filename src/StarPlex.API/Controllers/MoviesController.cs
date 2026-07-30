using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Features.Movies.DTOs;
using StarPlex.Application.Features.Movies.Queries.GetMovies;
using StarPlex.Application.Features.Movies.Queries.GetMovieById;
using StarPlex.Application.Features.Movies.Queries.SearchMovies;
using StarPlex.Application.Features.Movies.Commands.CreateMovie;
using StarPlex.Application.Features.Movies.Commands.UpdateMovie;
using StarPlex.Application.Features.Movies.Commands.DeleteMovie;
using StarPlex.Domain.Enums;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MoviesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<MovieDto>>> GetMovies([FromQuery] MovieStatus? status, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMoviesQuery { Status = status }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<MovieDto>> GetMovieById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMovieByIdQuery { Id = id }, cancellationToken);
        if (result == null) return NotFound($"Movie with ID '{id}' was not found.");
        return Ok(result);
    }

    [HttpGet("search-tmdb")]
    [Authorize(Roles = "CinemaManager,SuperAdmin")]
    public async Task<ActionResult<List<TmdbMovieResultDto>>> SearchTmdb([FromQuery] string query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SearchMoviesQuery { Query = query }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("import-tmdb/{tmdbId:int}")]
    [Authorize(Roles = "CinemaManager,SuperAdmin")]
    public async Task<ActionResult<Guid>> ImportMovie([FromRoute] int tmdbId, CancellationToken cancellationToken)
    {
        var command = new CreateMovieCommand { TmdbId = tmdbId };
        var movieId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetMovieById), new { id = movieId }, movieId);
    }

    [HttpPost]
    [Authorize(Roles = "CinemaManager,SuperAdmin")]
    public async Task<ActionResult<Guid>> CreateMovie([FromForm] CreateMovieCommand command, IFormFile? posterFile, CancellationToken cancellationToken)
    {
        if (posterFile != null && posterFile.Length > 0)
        {
            command.PosterFileStream = posterFile.OpenReadStream();
            command.PosterFileName = posterFile.FileName;
        }

        var movieId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetMovieById), new { id = movieId }, movieId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "CinemaManager,SuperAdmin")]
    public async Task<IActionResult> UpdateMovie(Guid id, [FromForm] UpdateMovieCommand command, IFormFile? posterFile, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest("ID in URL does not match ID in request body.");

        if (posterFile != null && posterFile.Length > 0)
        {
            command.PosterFileStream = posterFile.OpenReadStream();
            command.PosterFileName = posterFile.FileName;
        }

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "CinemaManager,SuperAdmin")]
    public async Task<IActionResult> DeleteMovie(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteMovieCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}