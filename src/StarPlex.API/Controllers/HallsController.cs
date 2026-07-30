using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Features.Halls.Commands.CreateHall;
using StarPlex.Application.Features.Halls.Commands.DeleteHall;
using StarPlex.Application.Features.Halls.Commands.UpdateHall;
using StarPlex.Application.Features.Halls.Commands.UpdateSeatProperties;
using StarPlex.Application.Features.Halls.DTOs;
using StarPlex.Application.Features.Halls.Queries.GetHallById;
using StarPlex.Application.Features.Halls.Queries.GetHallsByCinemaId;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HallsController : ControllerBase
{
    private readonly ISender _sender;

    public HallsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HallDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HallDto>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetHallByIdQuery { Id = id };
        var result = await _sender.Send(query, cancellationToken);

        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("cinema/{cinemaId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<HallDto>))]
    public async Task<ActionResult<List<HallDto>>> GetByCinemaId([FromRoute] Guid cinemaId, CancellationToken cancellationToken)
    {
        var query = new GetHallsByCinemaIdQuery { CinemaId = cinemaId };
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,CinemaManager")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateHallCommand command, CancellationToken cancellationToken)
    {
        var hallId = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = hallId }, hallId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,CinemaManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateHallCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;

        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,CinemaManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var deleteCommand = new DeleteHallCommand { Id = id };

        await _sender.Send(deleteCommand, cancellationToken);
        return NoContent();
    }

    [HttpPut("seats/{seatId:guid}/properties")]
    [Authorize(Roles = "SuperAdmin,CinemaManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSeatProperties([FromRoute] Guid seatId, [FromBody] UpdateSeatPropertiesCommand command, CancellationToken cancellationToken)
    {
        command.SeatId = seatId;
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }
}