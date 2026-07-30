using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Features.Cinemas.Commands.CreateCinema;
using StarPlex.Application.Features.Cinemas.DTOs;
using StarPlex.Application.Features.Cinemas.Queries.GetCinemaById;
using StarPlex.Application.Features.Cinemas.Queries.GetCinemas;
using StarPlex.Application.Features.Cinemas.Commands.UpdateCinema;
using StarPlex.Application.Features.Cinemas.Commands.DeleteCinema;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CinemasController : ControllerBase
{
    private readonly ISender _sender;

    public CinemasController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets a list of all cinemas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CinemaDto>))]
    public async Task<ActionResult<List<CinemaDto>>> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetCinemasQuery();
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific cinema by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CinemaDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CinemaDto>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetCinemaByIdQuery { Id = id };
        var result = await _sender.Send(query, cancellationToken);

        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing cinema.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCinemaCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { message = "ID in path and ID in body tokens do not match." });
        }

        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a specific cinema.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteCinemaCommand { Id = id };
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Creates a new cinema.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCinemaCommand command, CancellationToken cancellationToken)
    {
        var cinemaId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = cinemaId }, cinemaId);
    }
}